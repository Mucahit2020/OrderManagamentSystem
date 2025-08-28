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

        // Sipariş tamamlandı eventi alındığında logla
        _logger.LogInformation("Sipariş tamamlandı eventi alındı: {OrderId}", orderCompleted.OrderId);

        try
        {
            // Sipariş tamamlandı eventini işle
            await _invoiceService.HandleOrderCompletedAsync(orderCompleted, context.CancellationToken);
            _logger.LogInformation("Sipariş tamamlandı eventi başarıyla işlendi: {OrderId}", orderCompleted.OrderId);
        }
        catch (Exception ex)
        {
            // Hata oluşursa logla ve tekrar fırlat
            _logger.LogError(ex, "Sipariş tamamlandı eventi işlenirken hata oluştu: {OrderId}", orderCompleted.OrderId);
            throw;
        }
    }
}
