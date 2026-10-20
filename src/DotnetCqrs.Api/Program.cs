using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Application.Orders.Commands;
using DotnetCqrs.Application.Orders.Queries;
using DotnetCqrs.Domain.Orders.Exceptions;
using DotnetCqrs.Infrastructure.EventStore;
using DotnetCqrs.Infrastructure.Outbox;
using DotnetCqrs.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);

// Registro de dependências de infraestrutura (Singleton para estado em memória consistente)
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<IOutboxRepository, InMemoryOutboxRepository>();
builder.Services.AddSingleton<OrderReadModelProjection>();
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Processador Transactional Outbox em segundo plano
builder.Services.AddHostedService<TransactionalOutboxProcessor>();

// Casos de uso / Handlers CQRS
builder.Services.AddTransient<CreateOrderCommandHandler>();
builder.Services.AddTransient<ProcessPaymentCommandHandler>();
builder.Services.AddTransient<ShipOrderCommandHandler>();
builder.Services.AddTransient<GetOrderByIdQueryHandler>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "dotnet-cqrs-event-sourcing-engine",
    author = "Felipe Madison (@FelipeMadson)",
    version = "1.0.0",
    architecture = "Clean Architecture + CQRS + Event Sourcing + Transactional Outbox",
    status = "healthy"
}));

// Endpoints CQRS - Comandos (Write Side)
app.MapPost("/api/orders", async (
    CreateOrderCommand command,
    CreateOrderCommandHandler handler,
    CancellationToken ct) =>
{
    try
    {
        var orderId = await handler.HandleAsync(command, ct);
        return Results.Created($"/api/orders/{orderId}", new { orderId, status = "Placed" });
    }
    catch (OrderDomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/orders/{id:guid}/pay", async (
    Guid id,
    ProcessPaymentRequest request,
    ProcessPaymentCommandHandler handler,
    CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(new ProcessPaymentCommand(
            OrderId: id,
            PaymentTransactionId: request.PaymentTransactionId,
            AmountPaid: request.AmountPaid,
            ExpectedVersion: request.ExpectedVersion
        ), ct);
        return Results.Ok(new { orderId = id, status = "PaymentProcessed" });
    }
    catch (ConcurrencyException ex)
    {
        return Results.Conflict(new { error = ex.Message, actualVersion = ex.ActualVersion });
    }
    catch (OrderDomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/orders/{id:guid}/ship", async (
    Guid id,
    ShipOrderRequest request,
    ShipOrderCommandHandler handler,
    CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(new ShipOrderCommand(
            OrderId: id,
            TrackingNumber: request.TrackingNumber,
            Carrier: request.Carrier,
            ExpectedVersion: request.ExpectedVersion
        ), ct);
        return Results.Ok(new { orderId = id, status = "Shipped" });
    }
    catch (ConcurrencyException ex)
    {
        return Results.Conflict(new { error = ex.Message, actualVersion = ex.ActualVersion });
    }
    catch (OrderDomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Endpoints CQRS - Consultas (Read Side)
app.MapGet("/api/orders/{id:guid}", async (
    Guid id,
    GetOrderByIdQueryHandler handler,
    CancellationToken ct) =>
{
    var order = await handler.HandleAsync(new GetOrderByIdQuery(id), ct);
    return order != null ? Results.Ok(order) : Results.NotFound(new { error = $"Pedido {id} não encontrado." });
});

app.MapGet("/api/orders/projections/summary", (OrderReadModelProjection projection) =>
{
    return Results.Ok(projection.GetAll());
});

app.MapGet("/api/outbox/status", (IOutboxRepository repo) =>
{
    if (repo is InMemoryOutboxRepository inMem)
    {
        return Results.Ok(new
        {
            totalMessages = inMem.TotalCount,
            processed = inMem.ProcessedCount,
            pending = inMem.PendingCount
        });
    }
    return Results.Ok(new { status = "operational" });
});

app.Run();

public record ProcessPaymentRequest(string PaymentTransactionId, decimal AmountPaid, long ExpectedVersion);
public record ShipOrderRequest(string TrackingNumber, string Carrier, long ExpectedVersion);

public partial class Program { }
