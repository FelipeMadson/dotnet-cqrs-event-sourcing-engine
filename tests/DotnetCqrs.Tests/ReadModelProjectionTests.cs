using FluentAssertions;
using Xunit;
using DotnetCqrs.Domain.Orders;
using DotnetCqrs.Domain.Orders.Events;
using DotnetCqrs.Infrastructure.Projections;

namespace DotnetCqrs.Tests;

public class ReadModelProjectionTests
{
    [Fact]
    public void HandleEvent_OrderPlacedAndPaymentAndShipped_ShouldReflectEventualConsistentState()
    {
        // Arrange
        var projection = new OrderReadModelProjection();
        var orderId = Guid.NewGuid();

        var placed = new OrderPlacedEvent(
            Guid.NewGuid(),
            orderId,
            0,
            DateTime.UtcNow,
            "cust_projection_test",
            new List<OrderItem>
            {
                new("sku_1", "Monitor 4K", 1, 600m),
                new("sku_2", "Arm Mount", 1, 80m)
            },
            680m
        );

        var payment = new OrderPaymentProcessedEvent(
            Guid.NewGuid(),
            orderId,
            1,
            DateTime.UtcNow.AddMinutes(2),
            "txn_stripe_999",
            680m
        );

        var shipped = new OrderShippedEvent(
            Guid.NewGuid(),
            orderId,
            2,
            DateTime.UtcNow.AddMinutes(5),
            "TRK-BRA-001",
            "Jadlog"
        );

        // Act & Assert step by step
        projection.HandleEvent(placed);
        var view1 = projection.GetById(orderId);
        view1.Should().NotBeNull();
        view1!.Status.Should().Be("Placed");
        view1.TotalAmount.Should().Be(680m);
        view1.TotalItems.Should().Be(2);

        projection.HandleEvent(payment);
        var view2 = projection.GetById(orderId);
        view2!.Status.Should().Be("PaymentProcessed");

        projection.HandleEvent(shipped);
        var view3 = projection.GetById(orderId);
        view3!.Status.Should().Be("Shipped");
        view3.TrackingNumber.Should().Be("TRK-BRA-001");
    }
}
