namespace OrderManagement.Common.Events;

public sealed record OrderCreated : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required Guid CustomerId { get; init; }

    public required List<OrderItemDto> Items { get; init; } = new();

    public required decimal TotalAmount { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public OrderCreated()
    {
        EventType = nameof(OrderCreated);
    }
}