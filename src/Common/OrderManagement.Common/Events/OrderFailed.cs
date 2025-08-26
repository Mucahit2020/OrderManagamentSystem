namespace OrderManagement.Common.Events;

public sealed record OrderFailed : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required string Reason { get; init; }

    public required string FailureType { get; init; } // "STOCK_INSUFFICIENT", "INVOICE_FAILED", etc.

    public DateTime FailedAt { get; init; } = DateTime.UtcNow;

    public OrderFailed()
    {
        EventType = nameof(OrderFailed);
    }
}