using OrderManagement.InvoiceService.DTOs;

namespace OrderManagement.InvoiceService.Interfaces;

public interface IExternalInvoiceService
{
    Task<ExternalInvoiceResponse> CreateInvoiceAsync(CreateExternalInvoiceRequest request, CancellationToken cancellationToken = default);
}
