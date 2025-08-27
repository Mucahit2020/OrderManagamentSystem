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
    /// Get invoice by order ID
    /// </summary>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByOrderId(Guid orderId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(orderId, cancellationToken);
        if (invoice == null)
            return NotFound($"Invoice not found for order ID: {orderId}");

        var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
        return Ok(invoiceDto);
    }

    /// <summary>
    /// Get invoice by invoice ID
    /// </summary>
    [HttpGet("{invoiceId:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
            return NotFound($"Invoice not found: {invoiceId}");

        var invoiceDto = _mapper.Map<InvoiceDto>(invoice);
        return Ok(invoiceDto);
    }
}