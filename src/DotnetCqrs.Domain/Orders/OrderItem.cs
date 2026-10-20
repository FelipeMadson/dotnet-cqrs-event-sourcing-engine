namespace DotnetCqrs.Domain.Orders;

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
)
{
    public decimal TotalPrice => Quantity * UnitPrice;
}
