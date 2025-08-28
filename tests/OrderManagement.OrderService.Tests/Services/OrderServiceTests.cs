using AutoMapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Common.Events;
using OrderManagement.Common.Models;
using OrderManagement.OrderService.DTOs;
using OrderManagement.OrderService.Interfaces;
using OrderManagement.OrderService.Models;
using OrderManagement.OrderService.Services;
using Xunit;

namespace OrderManagement.OrderService.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<OrderManagement.OrderService.Services.OrderService>> _mockLogger;
        private readonly OrderManagement.OrderService.Services.OrderService _orderService;

        public OrderServiceTests()
        {
            // Mock'ları hazırla
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<OrderManagement.OrderService.Services.OrderService>>();

            // Test edilecek servisi oluştur
            _orderService = new OrderManagement.OrderService.Services.OrderService(
                _mockOrderRepository.Object,
                _mockPublishEndpoint.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateOrderAsync_IdempotencyKey_Exists_Should_Return_ExistingOrder()
        {
            // Arrange - Hazırlık
            var idempotencyKey = "test-key-123";
            var existingOrder = new Order
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                CustomerId = Guid.NewGuid()
            };

            var expectedResponse = new OrderResponse
            {
                Id = existingOrder.Id,
                IdempotencyKey = idempotencyKey
            };

            // Mock repository'nin nasıl davranacağını ayarla
            _mockOrderRepository
                .Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingOrder);

            // Mock mapper'ın nasıl davranacağını ayarla
            _mockMapper
                .Setup(x => x.Map<OrderResponse>(existingOrder))
                .Returns(expectedResponse);

            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>()
            };

            // Act - Eylemi gerçekleştir
            var result = await _orderService.CreateOrderAsync(idempotencyKey, request);

            // Assert - Sonuçları kontrol et
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(idempotencyKey, result.IdempotencyKey);

            // Repository'nin doğru çağrıldığını kontrol et
            _mockOrderRepository.Verify(
                x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()),
                Times.Once
            );

            // Yeni sipariş oluşturulmadığını kontrol et
            _mockOrderRepository.Verify(
                x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateOrderAsync_NewOrder_Should_Create_And_Publish_Event()
        {
            // Arrange
            var idempotencyKey = "new-key-456";
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 100m
                    }
                }
            };

            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                IdempotencyKey = idempotencyKey
            };

            // OrderCreated event'ini doğru şekilde oluştur
            var orderCreatedEvent = new OrderCreated
            {
                OrderId = newOrder.Id,
                CustomerId = customerId,
                TotalAmount = 200m, // 2 * 100
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 100m
                    }
                }
            };

            var expectedResponse = new OrderResponse
            {
                Id = newOrder.Id,
                CustomerId = customerId,
                IdempotencyKey = idempotencyKey
            };

            // Mevcut sipariş yok
            _mockOrderRepository
                .Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order)null);

            // Mapper kurulumu
            _mockMapper
                .Setup(x => x.Map<Order>(request))
                .Returns(newOrder);

            _mockMapper
                .Setup(x => x.Map<OrderCreated>(newOrder))
                .Returns(orderCreatedEvent);

            _mockMapper
                .Setup(x => x.Map<OrderResponse>(newOrder))
                .Returns(expectedResponse);

            // Repository kurulumu - CreateAsync metodu Task<Order> döndürüyor
            _mockOrderRepository
                .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newOrder);

            // Event publisher kurulumu
            _mockPublishEndpoint
                .Setup(x => x.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.CreateOrderAsync(idempotencyKey, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(customerId, result.CustomerId);

            // Tüm çağrıları doğrula
            _mockOrderRepository.Verify(
                x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

            _mockPublishEndpoint.Verify(
                x => x.Publish(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }
    }
}