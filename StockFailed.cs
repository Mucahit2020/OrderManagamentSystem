namespace OrderManagement.Common.Events;

public sealed record StockFailed : BaseEvent
{
    public required Guid OrderId { get; init; }

    public required string Reason { get; init; }

    public required List<ProductStockDto> InsufficientItems { get; init; } = new();

    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

    public StockFailed()
    {
        EventType = nameof(StockFailed);
    }
}