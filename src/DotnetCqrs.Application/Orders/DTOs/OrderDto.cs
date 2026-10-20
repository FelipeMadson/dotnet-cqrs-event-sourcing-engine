namespace DotnetCqrs.Application.Orders.DTOs;

public record OrderItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record OrderDto(
    Guid Id,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items,
    string? PaymentTransactionId,
    string? TrackingNumber,
    string? Carrier,
    string? CancellationReason,
    long Version,
    DateTime CreatedAtUtc,
    DateTime? ShippedAtUtc
);
