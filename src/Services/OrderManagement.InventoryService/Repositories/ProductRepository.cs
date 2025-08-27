using Microsoft.EntityFrameworkCore;
using OrderManagement.InventoryService.Data;
using OrderManagement.InventoryService.Interfaces;
using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly InventoryContext _context;

    public ProductRepository(InventoryContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Product>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
