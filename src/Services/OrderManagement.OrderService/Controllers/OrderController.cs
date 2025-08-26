using Microsoft.AspNetCore.Mvc;
using OrderManagement.OrderService.DTOs;
using OrderManagement.OrderService.Services;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")][Required] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header is required");
        }

        var result = await _orderService.CreateOrderAsync(idempotencyKey, request, cancellationToken);

        // Check if this was an existing order (idempotency)
        var existingOrder = await _orderService.GetOrderByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder != null && existingOrder.Id != result.Id)
        {
            return Ok(existingOrder); // 200 OK for existing order
        }

        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result); // 201 Created for new order
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        return order != null ? Ok(order) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<OrderResponse>>> GetOrdersByCustomer(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
        return Ok(orders);
    }
}