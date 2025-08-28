using MassTransit;
using OrderManagement.Common.Events;
using OrderManagement.OrderService.Services;

namespace OrderManagement.OrderService.EventHandlers;

public class StockFailedHandler : IConsumer<StockFailed>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<StockFailedHandler> _logger;

    public StockFailedHandler(IOrderService orderService, ILogger<StockFailedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockFailed> context)
    {
        var stockFailed = context.Message;

        _logger.LogInformation("StockFailed eventi alındı: {OrderId}", stockFailed.OrderId);

        try
        {
            await _orderService.HandleStockFailedAsync(stockFailed, context.CancellationToken);
            _logger.LogInformation("StockFailed eventi başarıyla işlendi: {OrderId}", stockFailed.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockFailed eventi işlenirken hata oluştu: {OrderId}", stockFailed.OrderId);
            throw; // MassTransit’in retry mekanizmasının devreye girmesi için hatayı tekrar fırlat
        }
    }
}
