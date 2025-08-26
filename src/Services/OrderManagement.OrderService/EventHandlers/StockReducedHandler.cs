using MassTransit;
using OrderManagement.Common.Events;
using OrderManagement.OrderService.Services;

namespace OrderManagement.OrderService.EventHandlers;

public class StockReducedHandler : IConsumer<StockReduced>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<StockReducedHandler> _logger;

    public StockReducedHandler(IOrderService orderService, ILogger<StockReducedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReduced> context)
    {
        await _orderService.HandleStockReducedAsync(context.Message, context.CancellationToken);
    }
}