using OrderManagement.Common.Events;

namespace OrderManagement.InventoryService.Interfaces;

public interface IInventoryService
{
    Task HandleOrderCreatedAsync(OrderCreated orderCreated, CancellationToken cancellationToken = default);
    Task<bool> CheckStockAvailabilityAsync(List<OrderManagement.Common.Models.OrderItemDto> items, CancellationToken cancellationToken = default);
    Task<bool> ReduceStockAsync(Guid orderId, List<OrderManagement.Common.Models.OrderItemDto> items, CancellationToken cancellationToken = default);
}