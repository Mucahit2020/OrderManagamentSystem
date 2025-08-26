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
        _logger.LogInformation("Creating order with idempotency key: {IdempotencyKey}", idempotencyKey);

        // Check if order already exists (idempotency)
        var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder != null)
        {
            _logger.LogInformation("Order already exists for idempotency key: {IdempotencyKey}, OrderId: {OrderId}",
                idempotencyKey, existingOrder.Id);
            return _mapper.Map<OrderResponse>(existingOrder);
        }

        // Map DTO to Entity
        var order = _mapper.Map<Order>(request);
        order.IdempotencyKey = idempotencyKey;

        // Save to database
        await _orderRepository.CreateAsync(order, cancellationToken);
        _logger.LogInformation("Order created successfully: {OrderId}", order.Id);

        // Map and publish OrderCreated event
        var orderCreatedEvent = _mapper.Map<OrderCreated>(order);
        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
        _logger.LogInformation("OrderCreated event published for order: {OrderId}", order.Id);

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
        _logger.LogInformation("Handling StockReduced event for order: {OrderId}", stockReduced.OrderId);

        var order = await _orderRepository.GetByIdAsync(stockReduced.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order not found for StockReduced event: {OrderId}", stockReduced.OrderId);
            return;
        }

        // Update order status to Completed
        order.Status = OrderStatus.Completed;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order status updated to Completed: {OrderId}", order.Id);

        // Publish OrderCompleted event
        var orderCompletedEvent = new OrderCompleted
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount
        };

        await _publishEndpoint.Publish(orderCompletedEvent, cancellationToken);
        _logger.LogInformation("OrderCompleted event published for order: {OrderId}", order.Id);
    }

    public async Task HandleStockFailedAsync(StockFailed stockFailed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling StockFailed event for order: {OrderId}", stockFailed.OrderId);

        var order = await _orderRepository.GetByIdAsync(stockFailed.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order not found for StockFailed event: {OrderId}", stockFailed.OrderId);
            return;
        }

        // Update order status to Failed
        order.Status = OrderStatus.Failed;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order status updated to Failed: {OrderId}, Reason: {Reason}",
            order.Id, stockFailed.Reason);

        // Publish OrderFailed event
        var orderFailedEvent = new OrderFailed
        {
            OrderId = order.Id,
            Reason = stockFailed.Reason,
            FailureType = "STOCK_INSUFFICIENT"
        };

        await _publishEndpoint.Publish(orderFailedEvent, cancellationToken);
        _logger.LogInformation("OrderFailed event published for order: {OrderId}", order.Id);
    }
}