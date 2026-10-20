using System.Collections.Concurrent;
using System.Text.Json;
using DotnetCqrs.Application.Abstractions;
using DotnetCqrs.Domain.Common;
using DotnetCqrs.Domain.Orders.Events;

namespace DotnetCqrs.Infrastructure.Projections;

public record OrderSummaryView(
    Guid OrderId,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    int TotalItems,
    string? TrackingNumber,
    DateTime UpdatedAtUtc
);

/// <summary>
/// Projeção assíncrona do modelo de leitura (CQRS Read Model).
/// Mantém consistência eventual processando eventos publicados pelo barramento.
/// </summary>
public sealed class OrderReadModelProjection
{
    private readonly ConcurrentDictionary<Guid, OrderSummaryView> _views = new();

    public IReadOnlyCollection<OrderSummaryView> GetAll() => _views.Values.ToList().AsReadOnly();

    public OrderSummaryView? GetById(Guid orderId) =>
        _views.TryGetValue(orderId, out var view) ? view : null;

    public void HandleEvent(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent e:
                _views[e.AggregateId] = new OrderSummaryView(
                    OrderId: e.AggregateId,
                    CustomerId: e.CustomerId,
                    Status: "Placed",
                    TotalAmount: e.TotalAmount,
                    TotalItems: e.Items.Sum(i => i.Quantity),
                    TrackingNumber: null,
                    UpdatedAtUtc: e.OccurredOnUtc
                );
                break;

            case OrderPaymentProcessedEvent e:
                if (_views.TryGetValue(e.AggregateId, out var existingPayment))
                {
                    _views[e.AggregateId] = existingPayment with
                    {
                        Status = "PaymentProcessed",
                        UpdatedAtUtc = e.OccurredOnUtc
                    };
                }
                break;

            case OrderShippedEvent e:
                if (_views.TryGetValue(e.AggregateId, out var existingShipped))
                {
                    _views[e.AggregateId] = existingShipped with
                    {
                        Status = "Shipped",
                        TrackingNumber = e.TrackingNumber,
                        UpdatedAtUtc = e.OccurredOnUtc
                    };
                }
                break;

            case OrderCancelledEvent e:
                if (_views.TryGetValue(e.AggregateId, out var existingCancelled))
                {
                    _views[e.AggregateId] = existingCancelled with
                    {
                        Status = "Cancelled",
                        UpdatedAtUtc = e.OccurredOnUtc
                    };
                }
                break;
        }
    }
}
