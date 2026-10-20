using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Application.Abstractions;

/// <summary>
/// Contrato de persistência para fluxos imutáveis de eventos de domínio.
/// </summary>
public interface IEventStore
{
    Task AppendEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, long expectedVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task<T> LoadAggregateAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot, new();
}
