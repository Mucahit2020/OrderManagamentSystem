using AutoMapper;
using MassTransit;
using OrderManagement.Common.Enums;
using OrderManagement.Common.Events;
using OrderManagement.OrderService.DTOs;
using OrderManagement.OrderService.Interfaces;
using OrderManagement.OrderService.Models;

namespace OrderManagement.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(
    string idempotencyKey,
    CreateOrderRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Idempotency anahtarı {IdempotencyKey} ile sipariş oluşturuluyor", idempotencyKey);

        // Sipariş zaten var mı? (idempotency kontrolü)
        var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder != null)
        {
            _logger.LogInformation("Bu idempotency key için sipariş zaten mevcut: {IdempotencyKey}, SiparişId: {OrderId}",
                idempotencyKey, existingOrder.Id);
            return _mapper.Map<OrderResponse>(existingOrder);
        }

        var order = _mapper.Map<Order>(request);
        order.IdempotencyKey = idempotencyKey;

        await _orderRepository.CreateAsync(order, cancellationToken);
        _logger.LogInformation("Sipariş başarıyla oluşturuldu: {OrderId}", order.Id);

        // OrderCreated event'i publishle.
        var orderCreatedEvent = _mapper.Map<OrderCreated>(order);
        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
        _logger.LogInformation("OrderCreated eventi yayımlandı. SiparişId: {OrderId}", order.Id);

        return _mapper.Map<OrderResponse>(order);
    }


    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        return order != null ? _mapper.Map<OrderResponse>(order) : null;
    }

    public async Task<OrderResponse?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        return order != null ? _mapper.Map<OrderResponse>(order) : null;
    }

    public async Task<List<OrderResponse>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return _mapper.Map<List<OrderResponse>>(orders);
    }

    public async Task HandleStockReducedAsync(StockReduced stockReduced, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StockReduced event’i işleniyor: {OrderId}", stockReduced.OrderId);

        var order = await _orderRepository.GetByIdAsync(stockReduced.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("StockReduced eventi için sipariş bulunamadı: {OrderId}", stockReduced.OrderId);
            return;
        }

        // Sipariş durumunu Completed olarak güncelle
        order.Status = OrderStatus.Completed;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Sipariş durumu Completed olarak güncellendi: {OrderId}", order.Id);

        // OrderCompleted event’i publish et
        var orderCompletedEvent = new OrderCompleted
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount
        };

        await _publishEndpoint.Publish(orderCompletedEvent, cancellationToken);
        _logger.LogInformation("OrderCompleted eventi yayımlandı: {OrderId}", order.Id);
    }


    public async Task HandleStockFailedAsync(StockFailed stockFailed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StockFailed event’i işleniyor: {OrderId}", stockFailed.OrderId);

        var order = await _orderRepository.GetByIdAsync(stockFailed.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("StockFailed eventi için sipariş bulunamadı: {OrderId}", stockFailed.OrderId);
            return;
        }

        // Sipariş durumunu Failed olarak güncelle
        order.Status = OrderStatus.Failed;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Sipariş durumu Failed olarak güncellendi: {OrderId}, Sebep: {Reason}",
            order.Id, stockFailed.Reason);

        // OrderFailed event’i publish et
        var orderFailedEvent = new OrderFailed
        {
            OrderId = order.Id,
            Reason = stockFailed.Reason,
            FailureType = "STOCK_INSUFFICIENT"
        };

        await _publishEndpoint.Publish(orderFailedEvent, cancellationToken);
        _logger.LogInformation("OrderFailed eventi yayımlandı: {OrderId}", order.Id);
    }

}