using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Domain.Orders.Events;

public record OrderPlacedEvent(
    Guid EventId,
    Guid AggregateId,
    long Version,
    DateTime OccurredOnUtc,
    string CustomerId,
    IReadOnlyList<OrderItem> Items,
    decimal TotalAmount
) : IDomainEvent;
