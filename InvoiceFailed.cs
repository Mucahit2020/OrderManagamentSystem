namespace OrderManagement.Common.Events;

public sealed record InvoiceFailed : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required string Reason { get; init; }

    public DateTime FailedAt { get; init; } = DateTime.UtcNow;

    public InvoiceFailed()
    {
        EventType = nameof(InvoiceFailed);
    }
}