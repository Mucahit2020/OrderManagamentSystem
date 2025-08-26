namespace OrderManagement.Common.Events;

public sealed record StockReduced : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required List<StockMovementDto> StockMovements { get; init; } = new();

    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

    public StockReduced()
    {
        EventType = nameof(StockReduced);
    }
}