using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderManagement.API.Models;
using OrderManagement.API.Services;
using OrderManagement.Common.Enums;
using System.Net;
using System.Text.Json;
using Xunit;

namespace OrderManagement.API.Tests.Services
{
    public class OrderServiceClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<OrderServiceClient>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly OrderServiceClient _orderServiceClient;

        public OrderServiceClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<OrderServiceClient>>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:5001/")
            };

            _orderServiceClient = new OrderServiceClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateOrderAsync_Success_Should_Return_OrderResponse()
        {
            // Arrange
            var idempotencyKey = "test-key-123";
            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 100m
                    }
                }
            };

            var expectedResponse = new OrderResponse
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                IdempotencyKey = idempotencyKey,
                Items = new List<OrderItemResponse>
                {
                    new OrderItemResponse
                    {
                        Id = Guid.NewGuid(),
                        ProductId = request.Items[0].ProductId,
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 100m,
                        TotalPrice = 200m,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                TotalAmount = 200m,
                Status = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var responseJson = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().EndsWith("api/orders") &&
                        req.Headers.Contains("Idempotency-Key")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.CreateOrderAsync(idempotencyKey, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.CustomerId, result.CustomerId);
            Assert.Equal(expectedResponse.IdempotencyKey, result.IdempotencyKey);
            Assert.Equal(expectedResponse.TotalAmount, result.TotalAmount);

            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().EndsWith("api/orders") &&
                    req.Headers.GetValues("Idempotency-Key").First() == idempotencyKey),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateOrderAsync_HttpError_Should_Throw_HttpRequestException()
        {
            // Arrange
            var idempotencyKey = "test-key-456";
            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>()
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _orderServiceClient.CreateOrderAsync(idempotencyKey, request));
        }

        [Fact]
        public async Task CreateOrderAsync_InvalidResponse_Should_Throw_InvalidOperationException()
        {
            // Arrange
            var idempotencyKey = "test-key-789";
            var request = new CreateOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>()
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderServiceClient.CreateOrderAsync(idempotencyKey, request));

            Assert.Contains("deserialize edilemedi", exception.Message);
        }

        [Fact]
        public async Task GetOrderByIdAsync_Success_Should_Return_OrderResponse()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var expectedResponse = new OrderResponse
            {
                Id = orderId,
                CustomerId = Guid.NewGuid(),
                IdempotencyKey = string.Empty,
                TotalAmount = 150m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemResponse>()
            };

            var responseJson = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"api/orders/{orderId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.GetOrderByIdAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal(expectedResponse.CustomerId, result.CustomerId);
            Assert.Equal(expectedResponse.TotalAmount, result.TotalAmount);
        }

        [Fact]
        public async Task GetOrderByIdAsync_NotFound_Should_Return_Null()
        {
            // Arrange
            var orderId = Guid.NewGuid();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"api/orders/{orderId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.GetOrderByIdAsync(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderByIdAsync_HttpError_Should_Throw_HttpRequestException()
        {
            // Arrange
            var orderId = Guid.NewGuid();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal Server Error")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _orderServiceClient.GetOrderByIdAsync(orderId));
        }

        [Fact]
        public async Task GetOrdersByCustomerAsync_Success_Should_Return_OrderResponseList()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var expectedResponses = new List<OrderResponse>
            {
                new OrderResponse
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    IdempotencyKey = string.Empty,
                    TotalAmount = 100m,
                    Status = OrderStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = new List<OrderItemResponse>()
                },
                new OrderResponse
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    IdempotencyKey = string.Empty,
                    TotalAmount = 250m,
                    Status = OrderStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = new List<OrderItemResponse>()
                }
            };

            var responseJson = JsonSerializer.Serialize(expectedResponses, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"api/orders/customer/{customerId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.GetOrdersByCustomerAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, order => Assert.Equal(customerId, order.CustomerId));
        }

        [Fact]
        public async Task GetOrdersByCustomerAsync_EmptyResponse_Should_Return_EmptyList()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.GetOrdersByCustomerAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetOrdersByCustomerAsync_NullResponse_Should_Return_EmptyList()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _orderServiceClient.GetOrdersByCustomerAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}