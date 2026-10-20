using System.Text.Json;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Domain.Orders;

namespace DotnetCqrs.Application.Orders.Commands;

public record CreateOrderItemInput(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record CreateOrderCommand(
    string CustomerId,
    List<CreateOrderItemInput> Items
);

public sealed class CreateOrderCommandHandler
{
    private readonly IEventStore _eventStore;
    private readonly IOutboxRepository _outboxRepository;

    public CreateOrderCommandHandler(IEventStore eventStore, IOutboxRepository outboxRepository)
    {
        _eventStore = eventStore;
        _outboxRepository = outboxRepository;
    }

    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var orderId = Guid.NewGuid();
        var domainItems = command.Items.Select(i =>
            new OrderItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)
        ).ToList();

        var order = Order.Place(orderId, command.CustomerId, domainItems);
        var uncommitted = order.UncommittedEvents.ToList();

        // 1. Gravar eventos no Event Store com verificação otimista de versão (-1 para novo agregado)
        await _eventStore.AppendEventsAsync(order.Id, uncommitted, expectedVersion: -1, cancellationToken);

        // 2. Gravar mensagens no Transactional Outbox atomicamente
        foreach (var @event in uncommitted)
        {
            var outboxItem = new OutboxItem(
                Id: Guid.NewGuid(),
                AggregateId: order.Id,
                EventType: @event.GetType().Name,
                PayloadJson: JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOnUtc: @event.OccurredOnUtc
            );
            await _outboxRepository.SaveAsync(outboxItem, cancellationToken);
        }

        order.ClearUncommittedEvents();
        return order.Id;
    }
}
