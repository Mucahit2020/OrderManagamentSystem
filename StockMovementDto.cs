namespace OrderManagement.Common.Models;

public sealed record StockMovementDto
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required string MovementType { get; init; } // "RESERVED", "CONSUMED", "RELEASED"

    public DateTime MovementDate { get; init; } = DateTime.UtcNow;
}