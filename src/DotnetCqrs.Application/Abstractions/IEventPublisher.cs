using DotnetCqrs.Domain.Common;

namespace DotnetCqrs.Application.Abstractions;

/// <summary>
/// Contrato do publicador de eventos assíncrono para o barramento de mensageria / message broker.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishRawAsync(string eventType, string payloadJson, CancellationToken cancellationToken = default);
}
