using MassTransit;
using OrderManagement.Common.Events;
using OrderManagement.InventoryService.Interfaces;

namespace OrderManagement.InventoryService.EventHandlers;

public class OrderCreatedHandler : IConsumer<OrderCreated>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(IInventoryService inventoryService, ILogger<OrderCreatedHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var orderCreated = context.Message;

        _logger.LogInformation("Sipariş için OrderCreated eventi alındı: {OrderId}", orderCreated.OrderId);

        try
        {
            await _inventoryService.HandleOrderCreatedAsync(orderCreated, context.CancellationToken);
            _logger.LogInformation("OrderCreated event başarıyla işlendi: {OrderId}", orderCreated.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderCreated event işlenirken hata oluştu: {OrderId}", orderCreated.OrderId);
            throw;
        }
    }
}