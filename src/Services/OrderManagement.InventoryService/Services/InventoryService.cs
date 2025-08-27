using MassTransit;
using OrderManagement.Common.Enums;
using OrderManagement.Common.Events;
using OrderManagement.InventoryService.Interfaces;
using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Services;

public class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<InventoryService> logger)
    {
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task HandleOrderCreatedAsync(OrderCreated orderCreated, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling OrderCreated event for order: {OrderId}", orderCreated.OrderId);

        try
        {
            var canFulfill = await CheckStockAvailabilityAsync(orderCreated.Items, cancellationToken);

            if (canFulfill)
            {
                var success = await ReduceStockAsync(orderCreated.OrderId, orderCreated.Items, cancellationToken);

                if (success)
                {
                    var stockReduced = new StockReduced
                    {
                        OrderId = orderCreated.OrderId,
                        StockMovements = orderCreated.Items.Select(item => new OrderManagement.Common.Models.StockMovementDto
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity,
                            MovementType = "CONSUMED"
                        }).ToList()
                    };

                    await _publishEndpoint.Publish(stockReduced, cancellationToken);
                    _logger.LogInformation("StockReduced event published for order: {OrderId}", orderCreated.OrderId);
                }
                else
                {
                    await PublishStockFailedAsync(orderCreated.OrderId, "Failed to reduce stock", orderCreated.Items, cancellationToken);
                }
            }
            else
            {
                await PublishStockFailedAsync(orderCreated.OrderId, "Insufficient stock", orderCreated.Items, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderCreated event for order: {OrderId}", orderCreated.OrderId);
            await PublishStockFailedAsync(orderCreated.OrderId, $"Error: {ex.Message}", orderCreated.Items, cancellationToken);
        }
    }

    public async Task<bool> CheckStockAvailabilityAsync(List<OrderManagement.Common.Models.OrderItemDto> items, CancellationToken cancellationToken = default)
    {
        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
            {
                _logger.LogWarning("Insufficient stock for product: {ProductId}, Required: {Required}, Available: {Available}",
                    item.ProductId, item.Quantity, product?.StockQuantity ?? 0);
                return false;
            }
        }

        return true;
    }

    public async Task<bool> ReduceStockAsync(Guid orderId, List<OrderManagement.Common.Models.OrderItemDto> items, CancellationToken cancellationToken = default)
    {
        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        foreach (var item in items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
            {
                return false;
            }

            var previousQuantity = product.StockQuantity;
            product.StockQuantity -= item.Quantity;

            await _productRepository.UpdateAsync(product, cancellationToken);

            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                OrderId = orderId,
                MovementType = StockMovementType.Consumed,
                Quantity = item.Quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = product.StockQuantity,
                Reason = $"Order {orderId} processed"
            };

            await _stockMovementRepository.CreateAsync(stockMovement, cancellationToken);
        }

        return true;
    }

    private async Task PublishStockFailedAsync(Guid orderId, string reason, List<OrderManagement.Common.Models.OrderItemDto> items, CancellationToken cancellationToken)
    {
        var stockFailed = new StockFailed
        {
            OrderId = orderId,
            Reason = reason,
            InsufficientItems = items.Select(item => new OrderManagement.Common.Models.ProductStockDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                RequestedQuantity = item.Quantity,
                AvailableQuantity = 0 // This would need to be fetched from DB for accurate reporting
            }).ToList()
        };

        await _publishEndpoint.Publish(stockFailed, cancellationToken);
        _logger.LogInformation("StockFailed event published for order: {OrderId}", orderId);
    }
}