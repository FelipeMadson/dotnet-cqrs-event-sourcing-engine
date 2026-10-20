using System.Collections.Concurrent;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Infrastructure.EventStore;

/// <summary>
/// Implementação em memória thread-safe de Event Store com controle estrito de concorrência otimista.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _streams = new();
    private readonly object _syncRoot = new();

    public Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var stream = _streams.GetOrAdd(aggregateId, _ => new List<IDomainEvent>());
            long actualVersion = stream.Count == 0 ? -1 : stream[^1].Version;

            if (actualVersion != expectedVersion)
            {
                throw new ConcurrencyException(aggregateId, expectedVersion, actualVersion);
            }

            stream.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (_streams.TryGetValue(aggregateId, out var stream))
            {
                return Task.FromResult<IReadOnlyList<IDomainEvent>>(stream.ToList().AsReadOnly());
            }

            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }
    }

    public async Task<T> LoadAggregateAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default)
        where T : AggregateRoot, new()
    {
        var events = await GetEventsAsync(aggregateId, cancellationToken);
        if (events.Count == 0)
        {
            throw new KeyNotFoundException($"Agregado com ID {aggregateId} não encontrado no Event Store.");
        }

        var aggregate = new T();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }
}
