namespace OrderManagement.Common.Models;

public sealed record OrderItemDto
{
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public decimal TotalPrice => Quantity * UnitPrice;
}