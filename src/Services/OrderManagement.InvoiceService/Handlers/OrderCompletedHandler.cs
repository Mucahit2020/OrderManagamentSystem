using MassTransit;
using OrderManagement.Common.Events;
using OrderManagement.InvoiceService.Interfaces;

namespace OrderManagement.InvoiceService.EventHandlers;

public class OrderCompletedHandler : IConsumer<OrderCompleted>
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<OrderCompletedHandler> _logger;

    public OrderCompletedHandler(IInvoiceService invoiceService, ILogger<OrderCompletedHandler> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompleted> context)
    {
        var orderCompleted = context.Message;

        _logger.LogInformation("Received OrderCompleted event for order: {OrderId}", orderCompleted.OrderId);

        try
        {
            await _invoiceService.HandleOrderCompletedAsync(orderCompleted, context.CancellationToken);
            _logger.LogInformation("Successfully handled OrderCompleted event for order: {OrderId}", orderCompleted.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderCompleted event for order: {OrderId}", orderCompleted.OrderId);
            throw;
        }
    }
}
