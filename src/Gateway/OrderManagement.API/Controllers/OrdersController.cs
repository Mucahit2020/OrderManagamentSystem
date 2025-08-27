using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.Models;
using OrderManagement.API.Services;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderServiceClient _orderServiceClient;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderServiceClient orderServiceClient, ILogger<OrdersController> logger)
    {
        _orderServiceClient = orderServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <param name="idempotencyKey">Unique idempotency key to prevent duplicate orders</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), 201)]
    [ProducesResponseType(typeof(OrderResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")][Required] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received order creation request with idempotency key: {IdempotencyKey}", idempotencyKey);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header is required");
        }

        try
        {
            var order = await _orderServiceClient.CreateOrderAsync(idempotencyKey, request, cancellationToken);

            // Determine if this was a new order or existing order (idempotency)
            // This would require additional logic to differentiate 201 vs 200 responses
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Order Service for idempotency key: {IdempotencyKey}", idempotencyKey);
            return StatusCode(500, "Internal server error occurred while creating order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating order with idempotency key: {IdempotencyKey}", idempotencyKey);
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting order: {OrderId}", id);

        try
        {
            var order = await _orderServiceClient.GetOrderByIdAsync(id, cancellationToken);
            return order != null ? Ok(order) : NotFound($"Order with ID {id} not found");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Order Service for order: {OrderId}", id);
            return StatusCode(500, "Internal server error occurred while retrieving order");
        }
    }

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customer orders</returns>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(List<OrderResponse>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<OrderResponse>>> GetOrdersByCustomer(Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting orders for customer: {CustomerId}", customerId);

        try
        {
            var orders = await _orderServiceClient.GetOrdersByCustomerAsync(customerId, cancellationToken);
            return Ok(orders);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Order Service for customer: {CustomerId}", customerId);
            return StatusCode(500, "Internal server error occurred while retrieving customer orders");
        }
    }
}