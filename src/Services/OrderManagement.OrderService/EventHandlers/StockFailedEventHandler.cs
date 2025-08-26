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

        _logger.LogInformation("Received StockFailed event for order: {OrderId}", stockFailed.OrderId);

        try
        {
            await _orderService.HandleStockFailedAsync(stockFailed, context.CancellationToken);
            _logger.LogInformation("Successfully handled StockFailed event for order: {OrderId}", stockFailed.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StockFailed event for order: {OrderId}", stockFailed.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}