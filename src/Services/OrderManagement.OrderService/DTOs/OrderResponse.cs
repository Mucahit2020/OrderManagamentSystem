using OrderManagement.Common.Enums;

namespace OrderManagement.OrderService.DTOs;

public record OrderResponse
{
    public Guid Id { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<OrderItemResponse> Items { get; init; } = new();
}

public record OrderItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
}