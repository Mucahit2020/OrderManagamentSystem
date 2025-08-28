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
        _logger.LogInformation("Idempotency anahtarı {IdempotencyKey} ile sipariş oluşturma isteği alındı.", idempotencyKey);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key zorunludur!");
        }

        try
        {
            var order = await _orderServiceClient.CreateOrderAsync(idempotencyKey, request, cancellationToken);

            // Bu sipariş yeni mi yoksa daha önce oluşturulmuş mu (idempotency)?
            // 201 ile 200 yanıtlarını ayırt etmek için ek mantık gerekir.
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Sipariş Servisini çağırırken idempotency anahtarı {IdempotencyKey} için hata oluştu", idempotencyKey);
            return StatusCode(500, "Sipariş oluşturulurken dahili sunucu hatası meydana geldi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Idempotency anahtarı {IdempotencyKey} ile sipariş oluşturulurken beklenmeyen hata oluştu", idempotencyKey);
            return StatusCode(500, "Beklenmeyen bir hata oluştu");
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
        _logger.LogInformation("Sipariş alınıyor: {OrderId}", id);

        try
        {
            var order = await _orderServiceClient.GetOrderByIdAsync(id, cancellationToken);
            return order != null
                ? Ok(order)
                : NotFound($"ID'si {id} olan sipariş bulunamadı");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Order Service çağrılırken hata oluştu: {OrderId}", id);
            return StatusCode(500, "Sipariş alınırken dahili sunucu hatası oluştu");
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
    public async Task<ActionResult<List<OrderResponse>>> GetOrdersByCustomer(
    Guid customerId,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Müşteri için siparişler alınıyor: {CustomerId}", customerId);

        try
        {
            var orders = await _orderServiceClient.GetOrdersByCustomerAsync(customerId, cancellationToken);
            return Ok(orders);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Order Service çağrılırken hata oluştu: {CustomerId}", customerId);
            return StatusCode(500, "Müşteri siparişleri alınırken dahili sunucu hatası oluştu");
        }
    }

}