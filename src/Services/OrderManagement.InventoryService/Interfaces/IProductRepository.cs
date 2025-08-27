using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}