namespace OrderManagement.InvoiceService.DTOs
{
    public class CreateExternalInvoiceResponse
    {
        public bool Success { get; set; }
        public string? InvoiceId { get; set; }
        public string? Reference { get; set; }
        public string? ErrorMessage { get; set; }
    }
}