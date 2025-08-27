using OrderManagement.Common.Enums;

namespace OrderManagement.InvoiceService.DTOs;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public InvoiceStatus Status { get; init; }
    public string? ExternalInvoiceId { get; init; }
    public string? ExternalReference { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? FailureReason { get; init; }
    public List<InvoiceItemDto> Items { get; init; } = new();
}

public record InvoiceItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}