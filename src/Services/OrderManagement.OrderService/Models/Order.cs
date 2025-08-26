using OrderManagement.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Created;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<OrderItem> Items { get; set; } = new();
}