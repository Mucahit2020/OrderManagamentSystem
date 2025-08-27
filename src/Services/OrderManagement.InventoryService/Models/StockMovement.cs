using OrderManagement.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.InventoryService.Models;

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductId { get; set; }

    public Guid? OrderId { get; set; }

    [Required]
    public StockMovementType MovementType { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int PreviousQuantity { get; set; }

    [Required]
    public int NewQuantity { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = "SYSTEM";

    public Product Product { get; set; } = null!;
}