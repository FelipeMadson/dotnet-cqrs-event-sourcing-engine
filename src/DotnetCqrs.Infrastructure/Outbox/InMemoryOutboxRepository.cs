using System.Collections.Concurrent;
using DotnetCqrs.Application.Abstractions;

namespace DotnetCqrs.Infrastructure.Outbox;

/// <summary>
/// Repositório de Transactional Outbox thread-safe em memória.
/// </summary>
public sealed class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly ConcurrentDictionary<Guid, OutboxItem> _messages = new();

    public Task SaveAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _messages[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxItem>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pending = _messages.Values
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<OutboxItem>>(pending.AsReadOnly());
    }

    public Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_messages.TryGetValue(id, out var item))
        {
            _messages[id] = item with { ProcessedOnUtc = DateTime.UtcNow, Error = null };
        }

        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_messages.TryGetValue(id, out var item))
        {
            _messages[id] = item with { Error = error };
        }

        return Task.CompletedTask;
    }

    public int TotalCount => _messages.Count;
    public int ProcessedCount => _messages.Values.Count(m => m.ProcessedOnUtc != null);
    public int PendingCount => _messages.Values.Count(m => m.ProcessedOnUtc == null);
}
