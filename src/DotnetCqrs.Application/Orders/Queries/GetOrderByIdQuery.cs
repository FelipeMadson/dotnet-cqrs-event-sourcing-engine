using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Application.Orders.DTOs;
using DotnetCqrs.Domain.Orders;

namespace DotnetCqrs.Application.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId);

public sealed class GetOrderByIdQueryHandler
{
    private readonly IEventStore _eventStore;

    public GetOrderByIdQueryHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<OrderDto?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetEventsAsync(query.OrderId, cancellationToken);
        if (events == null || events.Count == 0)
            return null;

        var order = new Order();
        order.LoadFromHistory(events);

        return new OrderDto(
            Id: order.Id,
            CustomerId: order.CustomerId,
            Status: order.Status.ToString(),
            TotalAmount: order.TotalAmount,
            Items: order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList(),
            PaymentTransactionId: order.PaymentTransactionId,
            TrackingNumber: order.TrackingNumber,
            Carrier: order.Carrier,
            CancellationReason: order.CancellationReason,
            Version: order.Version,
            CreatedAtUtc: order.CreatedAtUtc,
            ShippedAtUtc: order.ShippedAtUtc
        );
    }
}
