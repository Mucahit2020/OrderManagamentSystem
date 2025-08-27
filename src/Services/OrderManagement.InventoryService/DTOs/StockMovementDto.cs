using OrderManagement.Common.Enums;

namespace OrderManagement.InventoryService.DTOs;

public record StockMovementDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? OrderId { get; init; }
    public StockMovementType MovementType { get; init; }
    public int Quantity { get; init; }
    public int PreviousQuantity { get; init; }
    public int NewQuantity { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public ProductDto? Product { get; init; }
}