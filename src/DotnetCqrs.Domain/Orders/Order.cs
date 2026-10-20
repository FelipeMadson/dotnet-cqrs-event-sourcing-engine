using DotnetCqrs.Domain.Common;
using DotnetCqrs.Domain.Orders.Events;
using DotnetCqrs.Domain.Orders.Exceptions;

namespace DotnetCqrs.Domain.Orders;

/// <summary>
/// Agregado Order governado por Event Sourcing puro.
/// Todas as mutações de estado ocorrem através da geração e aplicação de eventos imutáveis.
/// </summary>
public sealed class Order : AggregateRoot
{
    public string CustomerId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? Carrier { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ShippedAtUtc { get; private set; }

    /// <summary>
    /// Construtor vazio necessário para reconstrução a partir do histórico de eventos.
    /// </summary>
    public Order() { }

    /// <summary>
    /// Fábrica para criação de um novo pedido via comando de negócio.
    /// </summary>
    public static Order Place(Guid orderId, string customerId, List<OrderItem> items)
    {
        if (orderId == Guid.Empty)
            throw new OrderDomainException("O identificador do pedido não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(customerId))
            throw new OrderDomainException("O identificador do cliente é obrigatório.");
        if (items == null || items.Count == 0)
            throw new OrderDomainException("O pedido deve conter pelo menos um item.");

        var total = items.Sum(i => i.TotalPrice);
        if (total <= 0)
            throw new OrderDomainException("O valor total do pedido deve ser maior que zero.");

        var order = new Order();
        var placedEvent = new OrderPlacedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: orderId,
            Version: 0,
            OccurredOnUtc: DateTime.UtcNow,
            CustomerId: customerId,
            Items: items,
            TotalAmount: total
        );

        order.RaiseEvent(placedEvent);
        return order;
    }

    /// <summary>
    /// Processa o pagamento do pedido.
    /// Invariante: O pedido deve estar no status Placed e o valor pago deve corresponder ao total.
    /// </summary>
    public void ProcessPayment(string paymentTransactionId, decimal amountPaid)
    {
        if (Status != OrderStatus.Placed)
            throw new OrderDomainException($"Não é possível processar pagamento para pedido com status {Status}.");
        if (string.IsNullOrWhiteSpace(paymentTransactionId))
            throw new OrderDomainException("O ID da transação de pagamento é obrigatório.");
        if (amountPaid != TotalAmount)
            throw new OrderDomainException($"Valor pago ({amountPaid}) diverge do total do pedido ({TotalAmount}).");

        RaiseEvent(new OrderPaymentProcessedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Version: Version + 1,
            OccurredOnUtc: DateTime.UtcNow,
            PaymentTransactionId: paymentTransactionId,
            AmountPaid: amountPaid
        ));
    }

    /// <summary>
    /// Despacha o pedido para entrega.
    /// Invariante: O pagamento deve ter sido confirmado previamente.
    /// </summary>
    public void Ship(string trackingNumber, string carrier)
    {
        if (Status != OrderStatus.PaymentProcessed)
            throw new OrderDomainException($"Apenas pedidos com pagamento confirmado podem ser despachados. Status atual: {Status}.");
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new OrderDomainException("O código de rastreamento é obrigatório.");
        if (string.IsNullOrWhiteSpace(carrier))
            throw new OrderDomainException("A transportadora deve ser informada.");

        RaiseEvent(new OrderShippedEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Version: Version + 1,
            OccurredOnUtc: DateTime.UtcNow,
            TrackingNumber: trackingNumber,
            Carrier: carrier
        ));
    }

    /// <summary>
    /// Cancela o pedido.
    /// Invariante: Pedidos já despachados não podem ser cancelados diretamente.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new OrderDomainException("Não é permitido cancelar pedidos já despachados.");
        if (Status == OrderStatus.Cancelled)
            throw new OrderDomainException("O pedido já se encontra cancelado.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new OrderDomainException("O motivo de cancelamento é obrigatório.");

        RaiseEvent(new OrderCancelledEvent(
            EventId: Guid.NewGuid(),
            AggregateId: Id,
            Version: Version + 1,
            OccurredOnUtc: DateTime.UtcNow,
            Reason: reason
        ));
    }

    /// <summary>
    /// Mutação determinística do estado através do padrão When.
    /// </summary>
    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent e:
                Id = e.AggregateId;
                CustomerId = e.CustomerId;
                Items = new List<OrderItem>(e.Items);
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Placed;
                CreatedAtUtc = e.OccurredOnUtc;
                break;

            case OrderPaymentProcessedEvent e:
                PaymentTransactionId = e.PaymentTransactionId;
                Status = OrderStatus.PaymentProcessed;
                break;

            case OrderShippedEvent e:
                TrackingNumber = e.TrackingNumber;
                Carrier = e.Carrier;
                ShippedAtUtc = e.OccurredOnUtc;
                Status = OrderStatus.Shipped;
                break;

            case OrderCancelledEvent e:
                CancellationReason = e.Reason;
                Status = OrderStatus.Cancelled;
                break;

            default:
                throw new InvalidOperationException($"Evento desconhecido: {@event.GetType().Name}");
        }
    }
}
