using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Domain.Orders.Events;

public record OrderCancelledEvent(
    Guid EventId,
    Guid AggregateId,
    long Version,
    DateTime OccurredOnUtc,
    string Reason
) : IDomainEvent;
