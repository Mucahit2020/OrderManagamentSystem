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
        _logger.LogInformation("Handling OrderCompleted event for order: {OrderId}", orderCompleted.OrderId);

        try
        {
            // Check if invoice already exists (idempotency)
            var existingInvoice = await _invoiceRepository.GetByOrderIdAsync(orderCompleted.OrderId, cancellationToken);
            if (existingInvoice != null)
            {
                _logger.LogInformation("Invoice already exists for order: {OrderId}, InvoiceId: {InvoiceId}",
                    orderCompleted.OrderId, existingInvoice.Id);
                return;
            }

            // Generate invoice number
            var invoiceNumber = GenerateInvoiceNumber();

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = orderCompleted.OrderId,
                InvoiceNumber = invoiceNumber,
                CustomerId = orderCompleted.CustomerId,
                Amount = orderCompleted.TotalAmount,
                Status = InvoiceStatus.Pending
            };

            // Save to database first
            await _invoiceRepository.CreateAsync(invoice, cancellationToken);

            // Call external service to create invoice
            var externalRequest = new CreateExternalInvoiceRequest
            {
                OrderId = orderCompleted.OrderId,
                CustomerId = orderCompleted.CustomerId,
                Amount = orderCompleted.TotalAmount,
                Items = new List<ExternalInvoiceItemRequest>() // Would map from order items in real scenario
            };

            var externalResponse = await _externalInvoiceService.CreateInvoiceAsync(externalRequest, cancellationToken);

            if (externalResponse.Success)
            {
                // Update invoice with external reference
                invoice.Status = InvoiceStatus.Created;
                invoice.ExternalInvoiceId = externalResponse.InvoiceId;
                invoice.ExternalReference = externalResponse.Reference;
                invoice.ProcessedAt = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // Publish InvoiceCreated event
                var invoiceCreatedEvent = new InvoiceCreated
                {
                    OrderId = invoice.OrderId,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    Amount = invoice.Amount
                };

                await _publishEndpoint.Publish(invoiceCreatedEvent, cancellationToken);
                _logger.LogInformation("InvoiceCreated event published for order: {OrderId}", orderCompleted.OrderId);
            }
            else
            {
                // Update invoice with failure
                invoice.Status = InvoiceStatus.Failed;
                invoice.FailureReason = externalResponse.ErrorMessage;
                invoice.ProcessedAt = DateTime.UtcNow;

                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // Publish InvoiceFailed event
                var invoiceFailedEvent = new InvoiceFailed
                {
                    OrderId = invoice.OrderId,
                    Reason = externalResponse.ErrorMessage ?? "Unknown error occurred during invoice creation"
                };

                await _publishEndpoint.Publish(invoiceFailedEvent, cancellationToken);
                _logger.LogError("Invoice creation failed for order: {OrderId}, Reason: {Reason}",
                    orderCompleted.OrderId, externalResponse.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderCompleted event for order: {OrderId}", orderCompleted.OrderId);

            // Publish InvoiceFailed event
            var invoiceFailedEvent = new InvoiceFailed
            {
                OrderId = orderCompleted.OrderId,
                Reason = $"Internal error: {ex.Message}"
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