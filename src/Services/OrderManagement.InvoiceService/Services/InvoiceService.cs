using AutoMapper;
using MassTransit;
using OrderManagement.Common.Enums;
using OrderManagement.Common.Events;
using OrderManagement.InvoiceService.DTOs;
using OrderManagement.InvoiceService.Interfaces;
using OrderManagement.InvoiceService.Models;

namespace OrderManagement.InvoiceService.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IExternalInvoiceService _externalInvoiceService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IExternalInvoiceService externalInvoiceService,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _externalInvoiceService = externalInvoiceService;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task HandleOrderCompletedAsync(OrderCompleted orderCompleted, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OrderCompleted eventi işleniyor: {OrderId}", orderCompleted.OrderId);

        try
        {
            // Fatura zaten var mı kontrol et (idempotency)
            var existingInvoice = await _invoiceRepository.GetByOrderIdAsync(orderCompleted.OrderId, cancellationToken);
            if (existingInvoice != null)
            {
                _logger.LogInformation("Sipariş için fatura zaten mevcut: {OrderId}, FaturaId: {InvoiceId}",
                    orderCompleted.OrderId, existingInvoice.Id);
                return;
            }

            // Fatura numarası oluştur
            var invoiceNumber = GenerateInvoiceNumber();

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = orderCompleted.OrderId,
                InvoiceNumber = invoiceNumber,
                CustomerId = orderCompleted.CustomerId,
                Amount = orderCompleted.TotalAmount,
                Status = InvoiceStatus.Pending
            };

            await _invoiceRepository.CreateAsync(invoice, cancellationToken);

            var externalRequest = new CreateExternalInvoiceRequest
            {
                OrderId = orderCompleted.OrderId,
                CustomerId = orderCompleted.CustomerId,
                Amount = orderCompleted.TotalAmount,
                Items = new List<ExternalInvoiceItemRequest>() 
            };

            var externalResponse = await _externalInvoiceService.CreateInvoiceAsync(externalRequest, cancellationToken);

            if (externalResponse.Success)
            {
                // Faturayı güncelle (başarılı)
                invoice.Status = InvoiceStatus.Created;
                invoice.ExternalInvoiceId = externalResponse.InvoiceId;
                invoice.ExternalReference = externalResponse.Reference;
                invoice.ProcessedAt = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // InvoiceCreated event’i publish et
                var invoiceCreatedEvent = new InvoiceCreated
                {
                    OrderId = invoice.OrderId,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Amount = invoice.Amount
                };

                await _publishEndpoint.Publish(invoiceCreatedEvent, cancellationToken);
                _logger.LogInformation("InvoiceCreated eventi publish edildi: {OrderId}", orderCompleted.OrderId);
            }
            else
            {
                // Faturayı başarısız olarak güncelle
                invoice.Status = InvoiceStatus.Failed;
                invoice.FailureReason = externalResponse.ErrorMessage;
                invoice.ProcessedAt = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // InvoiceFailed event’i publish et
                var invoiceFailedEvent = new InvoiceFailed
                {
                    OrderId = invoice.OrderId,
                    Reason = externalResponse.ErrorMessage ?? "Fatura oluşturulurken bilinmeyen bir hata oluştu"
                };

                await _publishEndpoint.Publish(invoiceFailedEvent, cancellationToken);
                _logger.LogError("Fatura oluşturulamadı: {OrderId}, Sebep: {Reason}",
                    orderCompleted.OrderId, externalResponse.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderCompleted eventi işlenirken hata oluştu: {OrderId}", orderCompleted.OrderId);

            // Hata durumunda InvoiceFailed event publish et
            var invoiceFailedEvent = new InvoiceFailed
            {
                OrderId = orderCompleted.OrderId,
                Reason = $"Dahili hata: {ex.Message}"
            };

            await _publishEndpoint.Publish(invoiceFailedEvent, cancellationToken);
        }
    }


    public async Task<Invoice?> GetInvoiceByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.GetByOrderIdAsync(orderId, cancellationToken);
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
    }

    private static string GenerateInvoiceNumber()
    {
        var today = DateTime.UtcNow;
        var timestamp = today.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }
}