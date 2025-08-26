using System.ComponentModel.DataAnnotations;
using OrderManagement.Common.Attributes;

namespace OrderManagement.OrderService.DTOs;

public record CreateOrderRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemRequest> Items { get; init; } = new();
}

public record CreateOrderItemRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid ProductId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string ProductName { get; init; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; init; }
}