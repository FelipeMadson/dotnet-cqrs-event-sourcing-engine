namespace DotnetCqrs.Domain.Common;

/// <summary>
/// Raiz de agregação orientada a Event Sourcing.
/// Garante que o estado interno seja derivado unicamente do histórico imutável de eventos.
/// </summary>
public abstract class AggregateRoot : Entity<Guid>
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public long Version { get; protected set; } = -1;

    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Aplica um novo evento de domínio gerado por um comando de negócio.
    /// Incrementa a versão e registra o evento na lista de eventos pendentes de persistência.
    /// </summary>
    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ApplyEvent(domainEvent);
        _uncommittedEvents.Add(domainEvent);
    }

    /// <summary>
    /// Reconstrói o estado do agregado a partir do fluxo histórico de eventos (Deterministic Event Replay).
    /// </summary>
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event);
            Version = @event.Version;
        }
    }

    /// <summary>
    /// Roteamento polimórfico interno de mutação de estado.
    /// </summary>
    protected abstract void When(IDomainEvent @event);

    private void ApplyEvent(IDomainEvent @event)
    {
        When(@event);
        Version++;
    }
}
