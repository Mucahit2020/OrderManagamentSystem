using OrderManagement.InventoryService.DTOs;

namespace OrderManagement.InventoryService.Interfaces;

public interface IProductService
{
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetAllActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetProductsByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
