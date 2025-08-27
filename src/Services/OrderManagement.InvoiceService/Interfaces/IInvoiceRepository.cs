using OrderManagement.InvoiceService.Models;

namespace OrderManagement.InvoiceService.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
