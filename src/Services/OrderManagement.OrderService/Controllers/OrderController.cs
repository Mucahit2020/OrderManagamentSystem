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

    /// <summary>
    /// Yeni bir sipariş oluşturur
    /// </summary>
    /// <param name="request">Sipariş oluşturma isteği</param>
    /// <param name="idempotencyKey">Tekrar deneme durumlarında idempotency anahtarı</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Oluşturulan veya daha önce oluşturulmuş sipariş</returns>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
     [FromBody] CreateOrderRequest request,
     [FromHeader(Name = "Idempotency-Key")][Required] string idempotencyKey,
     CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header zorunludur");
        }

        var result = await _orderService.CreateOrderAsync(idempotencyKey, request, cancellationToken);

        // Bu sipariş daha önce oluşturulmuş mu (idempotency kontrolü)
        var existingOrder = await _orderService.GetOrderByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder != null && existingOrder.Id != result.Id)
        {
            return Ok(existingOrder); // Daha önce oluşturulmuş sipariş için 200 OK
        }

        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result); // Yeni sipariş için 201 Created
    }

    /// <summary>
    /// ID'ye göre bir siparişi getirir
    /// </summary>
    /// <param name="id">Sipariş ID'si</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Sipariş bilgisi veya NotFound</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        return order != null ? Ok(order) : NotFound();
    }

    /// <summary>
    /// Müşteri ID'sine göre tüm siparişleri getirir
    /// </summary>
    /// <param name="customerId">Müşteri ID'si</param>
    /// <param name="cancellationToken">İptal tokeni</param>
    /// <returns>Müşteriye ait sipariş listesi</returns>
    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<List<OrderResponse>>> GetOrdersByCustomer(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
        return Ok(orders);
    }
}
