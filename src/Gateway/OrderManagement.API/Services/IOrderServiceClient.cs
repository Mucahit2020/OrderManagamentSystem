using OrderManagement.API.Models;

namespace OrderManagement.API.Services;

public interface IOrderServiceClient
{
    Task<OrderResponse> CreateOrderAsync(string idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<OrderResponse>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
