using FluentAssertions;
using Xunit;
using DotnetCqrs.Domain.Orders;
using DotnetCqrs.Domain.Orders.Events;
using DotnetCqrs.Domain.Orders.Exceptions;

namespace DotnetCqrs.Tests;

public class AggregateReplayTests
{
    [Fact]
    public void Place_ValidParameters_ShouldRaiseOrderPlacedEventAndSetState()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = "cust_001";
        var items = new List<OrderItem>
        {
            new("sku_keyboard", "Mechanical Keyboard", 1, 150.00m),
            new("sku_mouse", "Precision Mouse", 2, 75.00m)
        };

        // Act
        var order = Order.Place(orderId, customerId, items);

        // Assert
        order.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Placed);
        order.TotalAmount.Should().Be(300.00m);
        order.Version.Should().Be(0);
        order.UncommittedEvents.Should().HaveCount(1);
        order.UncommittedEvents.First().Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void LoadFromHistory_EventStream_ShouldReconstructStateDeterministically()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new("sku_laptop", "Developer Workstation", 1, 2500.00m)
        };

        var history = new List<DotnetCqrs.Domain.Common.IDomainEvent>
        {
            new OrderPlacedEvent(Guid.NewGuid(), orderId, 0, DateTime.UtcNow.AddMinutes(-10), "cust_999", items, 2500.00m),
            new OrderPaymentProcessedEvent(Guid.NewGuid(), orderId, 1, DateTime.UtcNow.AddMinutes(-5), "txn_stripe_abc123", 2500.00m),
            new OrderShippedEvent(Guid.NewGuid(), orderId, 2, DateTime.UtcNow.AddMinutes(-1), "TRK-987654", "FedEx Express")
        };

        // Act
        var replayedOrder = new Order();
        replayedOrder.LoadFromHistory(history);

        // Assert
        replayedOrder.Id.Should().Be(orderId);
        replayedOrder.CustomerId.Should().Be("cust_999");
        replayedOrder.Status.Should().Be(OrderStatus.Shipped);
        replayedOrder.TotalAmount.Should().Be(2500.00m);
        replayedOrder.PaymentTransactionId.Should().Be("txn_stripe_abc123");
        replayedOrder.TrackingNumber.Should().Be("TRK-987654");
        replayedOrder.Carrier.Should().Be("FedEx Express");
        replayedOrder.Version.Should().Be(2);
        replayedOrder.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Ship_WhenOrderNotPaid_ShouldThrowOrderDomainException()
    {
        // Arrange
        var order = Order.Place(Guid.NewGuid(), "cust_002", new List<OrderItem>
        {
            new("sku_item", "Test Item", 1, 50.00m)
        });

        // Act
        var act = () => order.Ship("TRK-000", "Correios");

        // Assert
        act.Should().Throw<OrderDomainException>()
            .WithMessage("*pagamento confirmado*");
    }

    [Fact]
    public void Cancel_WhenOrderAlreadyShipped_ShouldThrowOrderDomainException()
    {
        // Arrange
        var order = Order.Place(Guid.NewGuid(), "cust_003", new List<OrderItem>
        {
            new("sku_item", "Test Item", 1, 100.00m)
        });
        order.ProcessPayment("txn_001", 100.00m);
        order.Ship("TRK-123", "DHL");

        // Act
        var act = () => order.Cancel("Cliente desistiu");

        // Assert
        act.Should().Throw<OrderDomainException>()
            .WithMessage("*despachados*");
    }
}
