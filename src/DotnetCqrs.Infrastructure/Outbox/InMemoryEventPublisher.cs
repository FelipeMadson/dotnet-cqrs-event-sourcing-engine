using System.Text.Json;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Domain.Common;
using DotnetCqrs.Domain.Orders.Events;
using DotnetCqrs.Infrastructure.Projections;

namespace DotnetCqrs.Infrastructure.Outbox;

/// <summary>
/// Barramento de eventos em memória para testes e execução local, alimentando as projeções de leitura.
/// </summary>
public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly OrderReadModelProjection _readModelProjection;
    private readonly List<IDomainEvent> _publishedEvents = new();
    private readonly object _syncRoot = new();

    public InMemoryEventPublisher(OrderReadModelProjection readModelProjection)
    {
        _readModelProjection = readModelProjection;
    }

    public IReadOnlyList<IDomainEvent> PublishedEvents
    {
        get
        {
            lock (_syncRoot) return _publishedEvents.ToList().AsReadOnly();
        }
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _publishedEvents.Add(domainEvent);
        }

        _readModelProjection.HandleEvent(domainEvent);
        return Task.CompletedTask;
    }

    public Task PublishRawAsync(string eventType, string payloadJson, CancellationToken cancellationToken = default)
    {
        IDomainEvent? domainEvent = eventType switch
        {
            nameof(OrderPlacedEvent) => JsonSerializer.Deserialize<OrderPlacedEvent>(payloadJson),
            nameof(OrderPaymentProcessedEvent) => JsonSerializer.Deserialize<OrderPaymentProcessedEvent>(payloadJson),
            nameof(OrderShippedEvent) => JsonSerializer.Deserialize<OrderShippedEvent>(payloadJson),
            nameof(OrderCancelledEvent) => JsonSerializer.Deserialize<OrderCancelledEvent>(payloadJson),
            _ => null
        };

        if (domainEvent != null)
        {
            return PublishAsync(domainEvent, cancellationToken);
        }

        return Task.CompletedTask;
    }
}
