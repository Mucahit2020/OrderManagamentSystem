using OrderManagement.InvoiceService.DTOs;
using OrderManagement.InvoiceService.Interfaces;

namespace OrderManagement.InvoiceService.ExternalServices;

public class MockExternalInvoiceService : IExternalInvoiceService
{
    private readonly ILogger<MockExternalInvoiceService> _logger;
    private readonly Random _random = new();

    public MockExternalInvoiceService(ILogger<MockExternalInvoiceService> logger)
    {
        _logger = logger;
    }

    public async Task<ExternalInvoiceResponse> CreateInvoiceAsync(CreateExternalInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating external invoice for order: {OrderId}", request.OrderId);

        // Simulate external API call delay
        await Task.Delay(500, cancellationToken);

        // Simulate success/failure scenarios (90% success rate)
        var success = _random.NextDouble() > 0.1;

        if (success)
        {
            var externalInvoiceId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            var invoiceNumber = $"EXT-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";
            var reference = $"REF-{externalInvoiceId}";

            _logger.LogInformation("External invoice created successfully: {ExternalInvoiceId}", externalInvoiceId);

            return new ExternalInvoiceResponse
            {
                InvoiceId = externalInvoiceId,
                InvoiceNumber = invoiceNumber,
                Reference = reference,
                Success = true
            };
        }
        else
        {
            var errorMessage = "External invoice service temporarily unavailable";
            _logger.LogWarning("External invoice creation failed for order: {OrderId}, Error: {Error}",
                request.OrderId, errorMessage);

            return new ExternalInvoiceResponse
            {
                InvoiceId = string.Empty,
                InvoiceNumber = string.Empty,
                Reference = string.Empty,
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}