using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderManagement.Common.Configuration;
using OrderManagement.OrderService.Data;
using OrderManagement.OrderService.EventHandlers;
using OrderManagement.OrderService.Interfaces;
using OrderManagement.OrderService.Mappings;
using OrderManagement.OrderService.Repositories;
using OrderManagement.OrderService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/order-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "Order management microservice for Order Management System"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database Configuration
var databaseConfig = builder.Configuration.GetSection(DatabaseConfig.SectionName).Get<DatabaseConfig>();
builder.Services.AddDbContext<OrderContext>(options =>
{
    options.UseNpgsql(databaseConfig?.ConnectionString ??
        builder.Configuration.GetConnectionString("DefaultConnection"))
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .LogTo(Console.WriteLine, LogLevel.Information);
});

// AutoMapper Configuration (Invoice Service'deki gibi sýrayla)
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Repository Registration
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Service Registration
builder.Services.AddScoped<IOrderService, OrderManagement.OrderService.Services.OrderService>();

// MassTransit Configuration
var rabbitMqConfig = builder.Configuration.GetSection(RabbitMqConfig.SectionName).Get<RabbitMqConfig>();

builder.Services.AddMassTransit(x =>
{
    // Add consumers (event handlers)
    x.AddConsumer<StockReducedHandler>();
    x.AddConsumer<StockFailedHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqConfig?.Host ?? "rabbitmq", rabbitMqConfig?.VirtualHost ?? "/", h =>
        {
            h.Username(rabbitMqConfig?.Username ?? "admin");
            h.Password(rabbitMqConfig?.Password ?? "password123");
        });

        // Configure endpoints with naming conventions
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("order", false));

        // Configure retry policy
        cfg.UseRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
    });
});

// Health checks
var rabbitMqConnectionString =
    $"amqp://{rabbitMqConfig?.Username ?? "admin"}:{rabbitMqConfig?.Password ?? "password123"}@" +
    $"{rabbitMqConfig?.Host ?? "rabbitmq"}:{rabbitMqConfig?.Port ?? 5672}/{rabbitMqConfig?.VirtualHost ?? "/"}";

builder.Services.AddHealthChecks()
    .AddNpgSql(databaseConfig?.ConnectionString ?? builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddRabbitMQ(rabbitMqConnectionString, name: "rabbitmq", timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");


Log.Information("Order Service starting up...");

try
{
    await app.RunAsync(); // Bu satýr eksikti!
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}