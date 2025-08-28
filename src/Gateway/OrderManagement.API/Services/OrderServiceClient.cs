using OrderManagement.API.Models;
using System.Text;
using System.Text.Json;

namespace OrderManagement.API.Services;

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<OrderResponse> CreateOrderAsync(string idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Idempotency anahtarı {IdempotencyKey} ile Sipariş Servisi üzerinden sipariş oluşturuluyor", idempotencyKey);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/orders");
        httpRequest.Content = content;
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseJson, _jsonOptions);

        return orderResponse ?? throw new InvalidOperationException("Sipariş cevabı deserialize edilemedi");
    }


    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting order by ID: {OrderId}", orderId);

        var response = await _httpClient.GetAsync($"api/orders/{orderId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OrderResponse>(responseJson, _jsonOptions);
    }

    public async Task<List<OrderResponse>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting orders for customer: {CustomerId}", customerId);

        var response = await _httpClient.GetAsync($"api/orders/customer/{customerId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<OrderResponse>>(responseJson, _jsonOptions) ?? new List<OrderResponse>();
    }
}
