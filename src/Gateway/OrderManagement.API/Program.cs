using Microsoft.OpenApi.Models;
using OrderManagement.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/api-gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Management API Gateway",
        Version = "v1",
        Description = "Main API Gateway for Order Management System",
        Contact = new OpenApiContact
        {
            Name = "Order Management Team"
        }
    });

    // Add Idempotency Key parameter globally
    c.AddSecurityDefinition("IdempotencyKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Idempotency-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "Unique key to prevent duplicate requests"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// HTTP Clients for Microservices
var orderServiceUrl = builder.Configuration.GetValue<string>("Services:OrderService:BaseUrl") ?? "http://localhost:5002";
var inventoryServiceUrl = builder.Configuration.GetValue<string>("Services:InventoryService:BaseUrl") ?? "http://localhost:5001";
var invoiceServiceUrl = builder.Configuration.GetValue<string>("Services:InvoiceService:BaseUrl") ?? "http://localhost:5003";

builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    client.BaseAddress = new Uri(orderServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "OrderManagement.API/1.0");
});

// Health checks for microservices
builder.Services.AddHealthChecks()
    .AddCheck("order-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck("inventory-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck("invoice-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API v1");
        c.RoutePrefix = "swagger"; // Swagger UI artýk /swagger altýnda
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

    logger.LogInformation("Request: {Method} {Path} - IdempotencyKey: {IdempotencyKey}",
        context.Request.Method, context.Request.Path, idempotencyKey ?? "None");

    await next.Invoke();
});

app.UseAuthorization();
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

Log.Information("API Gateway starting up...");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
