namespace OrderManagement.Common.Models;

public sealed record ProductStockDto
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required int RequestedQuantity { get; init; }

    public required int AvailableQuantity { get; init; }

    public int ShortfallQuantity => RequestedQuantity - AvailableQuantity;
}