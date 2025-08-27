using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Interfaces;

public interface IStockMovementRepository
{
    Task CreateAsync(StockMovement stockMovement, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}