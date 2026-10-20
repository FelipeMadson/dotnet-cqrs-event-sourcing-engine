using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Domain.Orders.Events;

public record OrderPaymentProcessedEvent(
    Guid EventId,
    Guid AggregateId,
    long Version,
    DateTime OccurredOnUtc,
    string PaymentTransactionId,
    decimal AmountPaid
) : IDomainEvent;
