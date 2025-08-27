using OrderManagement.Common.Events;
using OrderManagement.OrderService.DTOs;

namespace OrderManagement.OrderService.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(string idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task HandleStockReducedAsync(StockReduced stockReduced, CancellationToken cancellationToken = default);
    Task HandleStockFailedAsync(StockFailed stockFailed, CancellationToken cancellationToken = default);
}