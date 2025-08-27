namespace OrderManagement.InvoiceService.DTOs;

public record ExternalInvoiceResponse
{
    public required string InvoiceId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string Reference { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}