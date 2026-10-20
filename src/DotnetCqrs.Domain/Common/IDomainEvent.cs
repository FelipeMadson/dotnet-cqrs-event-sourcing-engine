namespace DotnetCqrs.Domain.Common;

/// <summary>
/// Contrato base imutável para todos os eventos de domínio do sistema.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }
    long Version { get; }
    DateTime OccurredOnUtc { get; }
}
