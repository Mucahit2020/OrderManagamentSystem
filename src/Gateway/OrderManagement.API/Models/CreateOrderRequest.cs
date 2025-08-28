using System.ComponentModel.DataAnnotations;
using OrderManagement.Common.Attributes;

namespace OrderManagement.API.Models;

public record CreateOrderRequest
{
    [Required]
    [NotEmptyGuid]
    public Guid CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Sipariş en az bir üründen oluşmalıdır")]
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
    [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır")]
    public int Quantity { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Birim fiyat 0’dan büyük olmalıdır")]
    public decimal UnitPrice { get; init; }
}
