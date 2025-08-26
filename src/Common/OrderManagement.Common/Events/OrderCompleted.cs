namespace OrderManagement.Common.Events;

public sealed record OrderCompleted : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required decimal TotalAmount { get; init; }

    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    public OrderCompleted()
    {
        EventType = nameof(OrderCompleted);
    }
}