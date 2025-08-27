namespace OrderManagement.InvoiceService.DTOs;

public record CreateExternalInvoiceRequest
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required List<ExternalInvoiceItemRequest> Items { get; init; } = new();
}

public record ExternalInvoiceItemRequest
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}