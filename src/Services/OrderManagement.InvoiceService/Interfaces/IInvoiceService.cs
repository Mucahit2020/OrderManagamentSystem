using OrderManagement.Common.Events;

namespace OrderManagement.InvoiceService.Interfaces;

public interface IInvoiceService
{
    Task HandleOrderCompletedAsync(OrderCompleted orderCompleted, CancellationToken cancellationToken = default);
    Task<Models.Invoice?> GetInvoiceByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Models.Invoice?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
