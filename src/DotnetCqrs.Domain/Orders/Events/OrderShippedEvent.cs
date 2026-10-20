using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Domain.Orders.Events;

public record OrderShippedEvent(
    Guid EventId,
    Guid AggregateId,
    long Version,
    DateTime OccurredOnUtc,
    string TrackingNumber,
    string Carrier
) : IDomainEvent;
