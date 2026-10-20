using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Application.Abstractions;

public record OutboxItem(
    Guid Id,
    Guid AggregateId,
    string EventType,
    string PayloadJson,
    DateTime OccurredOnUtc,
    DateTime? ProcessedOnUtc = null,
    string? Error = null
);

/// <summary>
/// Contrato do repositório Transactional Outbox para garantir entrega at-least-once de eventos distribuídos.
/// </summary>
public interface IOutboxRepository
{
    Task SaveAsync(OutboxItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxItem>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
