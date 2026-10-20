namespace DotnetCqrs.Domain.Orders;

public enum OrderStatus
{
    Placed = 1,
    PaymentProcessed = 2,
    Shipped = 3,
    Cancelled = 4
}
