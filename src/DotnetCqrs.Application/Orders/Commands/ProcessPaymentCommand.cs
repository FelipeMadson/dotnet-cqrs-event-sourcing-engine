using System.Text.Json;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Domain.Orders;

namespace DotnetCqrs.Application.Orders.Commands;

public record ProcessPaymentCommand(
    Guid OrderId,
    string PaymentTransactionId,
    decimal AmountPaid,
    long ExpectedVersion
);

public sealed class ProcessPaymentCommandHandler
{
    private readonly IEventStore _eventStore;
    private readonly IOutboxRepository _outboxRepository;

    public ProcessPaymentCommandHandler(IEventStore eventStore, IOutboxRepository outboxRepository)
    {
        _eventStore = eventStore;
        _outboxRepository = outboxRepository;
    }

    public async Task HandleAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _eventStore.LoadAggregateAsync<Order>(command.OrderId, cancellationToken);
        var currentVersion = order.Version;

        order.ProcessPayment(command.PaymentTransactionId, command.AmountPaid);
        var uncommitted = order.UncommittedEvents.ToList();

        await _eventStore.AppendEventsAsync(order.Id, uncommitted, expectedVersion: currentVersion, cancellationToken);

        foreach (var @event in uncommitted)
        {
            var outboxItem = new OutboxItem(
                Id: Guid.NewGuid(),
                AggregateId: order.Id,
                EventType: @event.GetType().Name,
                PayloadJson: JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOnUtc: @event.OccurredOnUtc
            );
            await _outboxRepository.SaveAsync(outboxItem, cancellationToken);
        }

        order.ClearUncommittedEvents();
    }
}
