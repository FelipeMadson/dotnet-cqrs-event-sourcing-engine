namespace DotnetCqrs.Infrastructure.EventStore;

public class ConcurrencyException : Exception
{
    public Guid AggregateId { get; }
    public long ExpectedVersion { get; }
    public long ActualVersion { get; }

    public ConcurrencyException(Guid aggregateId, long expectedVersion, long actualVersion)
        : base($"Conflito de concorrência otimista para o agregado {aggregateId}. Versão esperada: {expectedVersion}, Versão atual: {actualVersion}")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
