using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Application.Orders.Commands;
using DotnetCqrs.Infrastructure.EventStore;
using DotnetCqrs.Infrastructure.Outbox;
using DotnetCqrs.Infrastructure.Projections;

namespace DotnetCqrs.Tests;

public class TransactionalOutboxTests
{
    [Fact]
    public async Task CreateOrderCommand_ShouldSaveEventAndEnqueueOutboxMessage()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var outboxRepo = new InMemoryOutboxRepository();
        var handler = new CreateOrderCommandHandler(eventStore, outboxRepo);

        var command = new CreateOrderCommand(
            CustomerId: "cust_outbox_1",
            Items: new List<CreateOrderItemInput>
            {
                new("sku_ram", "16GB DDR5 RAM", 2, 80.00m)
            }
        );

        // Act
        var orderId = await handler.HandleAsync(command);

        // Assert
        orderId.Should().NotBeEmpty();

        // 1. Event Store deve conter o evento OrderPlacedEvent
        var events = await eventStore.GetEventsAsync(orderId);
        events.Should().HaveCount(1);
        events[0].AggregateId.Should().Be(orderId);

        // 2. Outbox deve conter 1 mensagem pendente
        var pending = await outboxRepo.GetPendingAsync(10);
        pending.Should().HaveCount(1);
        pending[0].AggregateId.Should().Be(orderId);
        pending[0].EventType.Should().Be("OrderPlacedEvent");
        pending[0].ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task OutboxProcessor_ShouldPublishPendingMessagesAndMarkAsProcessed()
    {
        // Arrange
        var projection = new OrderReadModelProjection();
        var publisher = new InMemoryEventPublisher(projection);
        var outboxRepo = new InMemoryOutboxRepository();
        var processor = new TransactionalOutboxProcessor(
            outboxRepo,
            publisher,
            NullLogger<TransactionalOutboxProcessor>.Instance
        );

        var outboxItem = new OutboxItem(
            Id: Guid.NewGuid(),
            AggregateId: Guid.NewGuid(),
            EventType: "OrderPlacedEvent",
            PayloadJson: "{\"EventId\":\"" + Guid.NewGuid() + "\",\"AggregateId\":\"" + Guid.NewGuid() + "\",\"Version\":0,\"OccurredOnUtc\":\"" + DateTime.UtcNow.ToString("o") + "\",\"CustomerId\":\"cust_test\",\"Items\":[],\"TotalAmount\":100}",
            OccurredOnUtc: DateTime.UtcNow
        );
        await outboxRepo.SaveAsync(outboxItem);

        // Act
        var processedCount = await processor.ProcessPendingMessagesAsync();

        // Assert
        processedCount.Should().Be(1);
        outboxRepo.PendingCount.Should().Be(0);
        outboxRepo.ProcessedCount.Should().Be(1);
        publisher.PublishedEvents.Should().HaveCount(1);
    }
}
