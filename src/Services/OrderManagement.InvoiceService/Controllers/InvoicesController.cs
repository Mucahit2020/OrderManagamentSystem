using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.InvoiceService.DTOs;
using OrderManagement.InvoiceService.Interfaces;

namespace OrderManagement.InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, IMapper mapper, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Sipariş ID'sine göre faturayı getir
    /// </summary>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByOrderId(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(orderId, cancellationToken);
        if (invoice == null)
            return NotFound($"Sipariş ID'si {orderId} için fatura bulunamadı");

        var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
        return Ok(invoiceDto);
    }

    /// <summary>
    /// Fatura ID'sine göre faturayı getir
    /// </summary>
    [HttpGet("{invoiceId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
            return NotFound($"ID'si {invoiceId} olan fatura bulunamadı");

        var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
        return Ok(invoiceDto);
    }
}
