using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Common.Enums;
using OrderManagement.Common.Events;
using OrderManagement.Common.Models;
using OrderManagement.InventoryService.Interfaces;
using OrderManagement.InventoryService.Models;
using OrderManagement.InventoryService.Services;
using Xunit;

namespace OrderManagement.InventoryService.Tests.Services
{
    public class InventoryServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IStockMovementRepository> _mockStockMovementRepository;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<ILogger<OrderManagement.InventoryService.Services.InventoryService>> _mockLogger;
        private readonly OrderManagement.InventoryService.Services.InventoryService _inventoryService;

        public InventoryServiceTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockStockMovementRepository = new Mock<IStockMovementRepository>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockLogger = new Mock<ILogger<OrderManagement.InventoryService.Services.InventoryService>>();

            _inventoryService = new OrderManagement.InventoryService.Services.InventoryService(
                _mockProductRepository.Object,
                _mockStockMovementRepository.Object,
                _mockPublishEndpoint.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_SufficientStock_Should_ReduceStock_And_Publish_StockReduced()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var orderCreated = new OrderCreated
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                TotalAmount = 200m,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 5,
                        UnitPrice = 40m
                    }
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 10
            };

            // Repository setup - stok yeterli
            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            _mockProductRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockStockMovementRepository
                .Setup(x => x.CreateAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockPublishEndpoint
                .Setup(x => x.Publish(It.IsAny<StockReduced>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.HandleOrderCreatedAsync(orderCreated);

            // Assert
            // Product update edilmeli (stok düşürülmeli)
            _mockProductRepository.Verify(
                x => x.UpdateAsync(It.Is<Product>(p => p.StockQuantity == 5), It.IsAny<CancellationToken>()),
                Times.Once
            );

            // StockMovement kaydı oluşturulmalı
            _mockStockMovementRepository.Verify(
                x => x.CreateAsync(It.Is<StockMovement>(sm =>
                    sm.ProductId == productId &&
                    sm.OrderId == orderId &&
                    sm.MovementType == StockMovementType.Consumed &&
                    sm.Quantity == 5 &&
                    sm.PreviousQuantity == 10 &&
                    sm.NewQuantity == 5),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );

            // StockReduced event publish edilmeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.Is<StockReduced>(sr =>
                    sr.OrderId == orderId &&
                    sr.StockMovements.Count == 1 &&
                    sr.StockMovements[0].ProductId == productId &&
                    sr.StockMovements[0].Quantity == 5),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );

            // StockFailed event publish edilmemeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.IsAny<StockFailed>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_InsufficientStock_Should_Publish_StockFailed()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var orderCreated = new OrderCreated
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                TotalAmount = 500m,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 15, // Stokta sadece 10 var
                        UnitPrice = 33.33m
                    }
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 10 // Yetersiz stok
            };

            // Repository setup
            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            _mockPublishEndpoint
                .Setup(x => x.Publish(It.IsAny<StockFailed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.HandleOrderCreatedAsync(orderCreated);

            // Assert
            // Product update edilmemeli
            _mockProductRepository.Verify(
                x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never
            );

            // StockMovement kaydı oluşturulmamalı
            _mockStockMovementRepository.Verify(
                x => x.CreateAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
                Times.Never
            );

            // StockFailed event publish edilmeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.Is<StockFailed>(sf =>
                    sf.OrderId == orderId &&
                    sf.Reason == "Yetersiz stok" &&
                    sf.InsufficientItems.Count == 1 &&
                    sf.InsufficientItems[0].ProductId == productId &&
                    sf.InsufficientItems[0].RequestedQuantity == 15),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );

            // StockReduced event publish edilmemeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.IsAny<StockReduced>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_ProductNotFound_Should_Publish_StockFailed()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var orderCreated = new OrderCreated
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                TotalAmount = 100m,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = productId,
                        ProductName = "Nonexistent Product",
                        Quantity = 1,
                        UnitPrice = 100m
                    }
                }
            };

            // Repository setup - ürün bulunamıyor
            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>()); // Boş liste

            _mockPublishEndpoint
                .Setup(x => x.Publish(It.IsAny<StockFailed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.HandleOrderCreatedAsync(orderCreated);

            // Assert
            // StockFailed event publish edilmeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.Is<StockFailed>(sf =>
                    sf.OrderId == orderId &&
                    sf.Reason == "Yetersiz stok"),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task HandleOrderCreatedAsync_Exception_Should_Publish_StockFailed()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orderCreated = new OrderCreated
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                TotalAmount = 100m,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        Quantity = 1,
                        UnitPrice = 100m
                    }
                }
            };

            // Repository exception fırlatıyor
            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            _mockPublishEndpoint
                .Setup(x => x.Publish(It.IsAny<StockFailed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.HandleOrderCreatedAsync(orderCreated);

            // Assert
            // StockFailed event publish edilmeli
            _mockPublishEndpoint.Verify(
                x => x.Publish(It.Is<StockFailed>(sf =>
                    sf.OrderId == orderId &&
                    sf.Reason.Contains("Database error")),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CheckStockAvailabilityAsync_SufficientStock_Should_Return_True()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 3,
                    UnitPrice = 50m
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 10
            };

            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            // Act
            var result = await _inventoryService.CheckStockAvailabilityAsync(items);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckStockAvailabilityAsync_InsufficientStock_Should_Return_False()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 15, // Stokta sadece 10 var
                    UnitPrice = 20m
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 10
            };

            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            // Act
            var result = await _inventoryService.CheckStockAvailabilityAsync(items);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReduceStockAsync_Success_Should_Update_Products_And_Create_StockMovements()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 7,
                    UnitPrice = 25m
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 20
            };

            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            _mockProductRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockStockMovementRepository
                .Setup(x => x.CreateAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _inventoryService.ReduceStockAsync(orderId, items);

            // Assert
            Assert.True(result);

            // Product güncellenmiş olmalı
            _mockProductRepository.Verify(
                x => x.UpdateAsync(It.Is<Product>(p => p.StockQuantity == 13), It.IsAny<CancellationToken>()),
                Times.Once
            );

            // StockMovement kaydı oluşturulmuş olmalı
            _mockStockMovementRepository.Verify(
                x => x.CreateAsync(It.Is<StockMovement>(sm =>
                    sm.ProductId == productId &&
                    sm.OrderId == orderId &&
                    sm.Quantity == 7 &&
                    sm.PreviousQuantity == 20 &&
                    sm.NewQuantity == 13),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task ReduceStockAsync_InsufficientStock_Should_Return_False()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 25, // Stokta sadece 20 var
                    UnitPrice = 10m
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                StockQuantity = 20
            };

            _mockProductRepository
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            // Act
            var result = await _inventoryService.ReduceStockAsync(orderId, items);

            // Assert
            Assert.False(result);

            // Hiçbir update yapılmamalı
            _mockProductRepository.Verify(
                x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never
            );

            // Hiçbir StockMovement kaydı oluşturulmamalı
            _mockStockMovementRepository.Verify(
                x => x.CreateAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }
    }
}