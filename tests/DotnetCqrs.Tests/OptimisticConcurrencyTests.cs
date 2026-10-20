using FluentAssertions;
using Xunit;
using DotnetCqrs.Domain.Orders;
using DotnetCqrs.Domain.Orders.Events;
using DotnetCqrs.Infrastructure.EventStore;

namespace DotnetCqrs.Tests;

public class OptimisticConcurrencyTests
{
    [Fact]
    public async Task AppendEventsAsync_WhenExpectedVersionMismatches_ShouldThrowConcurrencyException()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var orderId = Guid.NewGuid();

        var initialEvent = new OrderPlacedEvent(
            Guid.NewGuid(),
            orderId,
            0,
            DateTime.UtcNow,
            "cust_test",
            new List<OrderItem> { new("prod_1", "Test Product", 1, 100m) },
            100m
        );

        // Append initial version 0 (expectedVersion: -1)
        await eventStore.AppendEventsAsync(orderId, new[] { initialEvent }, expectedVersion: -1);

        var conflictingEvent = new OrderPaymentProcessedEvent(
            Guid.NewGuid(),
            orderId,
            1,
            DateTime.UtcNow,
            "txn_test",
            100m
        );

        // Act: Attempt to append claiming expected version is still -1 instead of 0
        var act = async () => await eventStore.AppendEventsAsync(orderId, new[] { conflictingEvent }, expectedVersion: -1);

        // Assert
        var exception = await act.Should().ThrowAsync<ConcurrencyException>();
        exception.Which.ExpectedVersion.Should().Be(-1);
        exception.Which.ActualVersion.Should().Be(0);
        exception.Which.AggregateId.Should().Be(orderId);
    }

    [Fact]
    public async Task AppendEventsAsync_WhenExpectedVersionMatches_ShouldSucceed()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var orderId = Guid.NewGuid();

        var event1 = new OrderPlacedEvent(Guid.NewGuid(), orderId, 0, DateTime.UtcNow, "cust_test", new List<OrderItem> { new("p", "P", 1, 50m) }, 50m);
        var event2 = new OrderPaymentProcessedEvent(Guid.NewGuid(), orderId, 1, DateTime.UtcNow, "txn_1", 50m);

        // Act & Assert
        await eventStore.AppendEventsAsync(orderId, new[] { event1 }, expectedVersion: -1);
        await eventStore.AppendEventsAsync(orderId, new[] { event2 }, expectedVersion: 0);

        var events = await eventStore.GetEventsAsync(orderId);
        events.Should().HaveCount(2);
    }
}
