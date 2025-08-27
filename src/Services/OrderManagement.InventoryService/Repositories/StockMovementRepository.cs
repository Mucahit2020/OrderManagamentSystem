using Microsoft.EntityFrameworkCore;
using OrderManagement.InventoryService.Data;
using OrderManagement.InventoryService.Interfaces;
using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly InventoryContext _context;

    public StockMovementRepository(InventoryContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(StockMovement stockMovement, CancellationToken cancellationToken = default)
    {
        await _context.StockMovements.AddAsync(stockMovement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<StockMovement>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.OrderId == orderId)
            .OrderBy(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
