namespace OrderManagement.Common.Events;

public sealed record InvoiceCreated : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required Guid InvoiceId { get; init; }

    public required string InvoiceNumber { get; init; }

    public required decimal Amount { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public InvoiceCreated()
    {
        EventType = nameof(InvoiceCreated);
    }
}