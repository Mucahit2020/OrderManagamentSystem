using Microsoft.EntityFrameworkCore;
using OrderManagement.InvoiceService.Data;
using OrderManagement.InvoiceService.Interfaces;
using OrderManagement.InvoiceService.Models;

namespace OrderManagement.InvoiceService.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceContext _context;

    public InvoiceRepository(InvoiceContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.OrderId == orderId, cancellationToken);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}