using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotnetCqrs.Application.Abstractions;

namespace DotnetCqrs.Infrastructure.Outbox;

/// <summary>
/// Processador em segundo plano do Transactional Outbox (Guarantees At-Least-Once Delivery).
/// Polling periódico para envio assíncrono ao Message Broker eliminando Dual-Write.
/// </summary>
public sealed class TransactionalOutboxProcessor : BackgroundService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransactionalOutboxProcessor> _logger;
    private readonly TimeSpan _pollInterval;

    public TransactionalOutboxProcessor(
        IOutboxRepository outboxRepository,
        IEventPublisher eventPublisher,
        ILogger<TransactionalOutboxProcessor> logger,
        TimeSpan? pollInterval = null)
    {
        _outboxRepository = outboxRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(500);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transactional Outbox Processor iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro durante processamento do lote do Transactional Outbox.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Transactional Outbox Processor finalizado.");
    }

    public async Task<int> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var batch = await _outboxRepository.GetPendingAsync(batchSize: 50, cancellationToken);
        int processedCount = 0;

        foreach (var message in batch)
        {
            try
            {
                await _eventPublisher.PublishRawAsync(message.EventType, message.PayloadJson, cancellationToken);
                await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);
                processedCount++;
                _logger.LogDebug("Mensagem Outbox {MessageId} ({EventType}) publicada com sucesso.", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao publicar mensagem Outbox {MessageId}. Marcando para reprocessamento.", message.Id);
                await _outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }

        return processedCount;
    }
}
