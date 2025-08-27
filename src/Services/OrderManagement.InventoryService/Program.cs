using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderManagement.Common.Configuration;
using OrderManagement.InventoryService.Data;
using OrderManagement.InventoryService.EventHandlers;
using OrderManagement.InventoryService.Interfaces;
using OrderManagement.InventoryService.Mappings;
using OrderManagement.InventoryService.Repositories;
using OrderManagement.InventoryService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory Service API",
        Version = "v1",
        Description = "Inventory management microservice for Order Management System"
    });

    // XML comments için (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
// Database Configuration
var databaseConfig = builder.Configuration.GetSection(DatabaseConfig.SectionName).Get<DatabaseConfig>();
builder.Services.AddDbContext<InventoryContext>(options =>
{
    options.UseNpgsql(databaseConfig?.ConnectionString ??
        builder.Configuration.GetConnectionString("DefaultConnection")).UseUpperSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .LogTo(Console.WriteLine, LogLevel.Information);
});

// Repository Registration
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IInventoryService, OrderManagement.InventoryService.Services.InventoryService>();
builder.Services.AddScoped<IProductService, ProductService>();

// MassTransit Configuration
// MassTransit Configuration
var rabbitMqConfig = builder.Configuration.GetSection(RabbitMqConfig.SectionName).Get<RabbitMqConfig>();

builder.Services.AddMassTransit(x =>
{
    // Add consumers (event handlers)
    x.AddConsumer<OrderCreatedHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqConfig?.Host ?? "rabbitmq", rabbitMqConfig?.VirtualHost ?? "/", h =>
        {
            h.Username(rabbitMqConfig?.Username ?? "admin");
            h.Password(rabbitMqConfig?.Password ?? "password123");
        });

        // Configure endpoints with naming conventions
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("inventory", false));

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Immediate(5));
    });
});

// Health checks with proper RabbitMQ connection string
var rabbitMqConnectionString =
    $"amqp://{rabbitMqConfig?.Username ?? "admin"}:{rabbitMqConfig?.Password ?? "password123"}@" +
    $"{rabbitMqConfig?.Host ?? "rabbitmq"}:{rabbitMqConfig?.Port ?? 5672}/{rabbitMqConfig?.VirtualHost ?? "/"}";

builder.Services.AddHealthChecks()
    .AddNpgSql(databaseConfig?.ConnectionString, name: "PostgreSQL Health Check");

builder.Services.AddAutoMapper(typeof(InventoryMappingProfile));

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

// Database migration and seeding on startup
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
//    try
//    {
//        //await context.Database.MigrateAsync();
//        //Log.Information("Database migration completed successfully");

//        // Seed initial data if needed
//        await SeedInitialDataAsync(context);
//    }
//    catch (Exception ex)
//    {
//        Log.Fatal(ex, "Database migration failed");
//        throw;
//    }
//}

Log.Information("Inventory Service starting up...");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

//static async Task SeedInitialDataAsync(InventoryContext context)
//{
//    if (!await context.Products.AnyAsync()) // Bu EF üzerinden kontrol ediyor, tablo adý önemli deðil
//    {
//        var products = new[]
//        {
//            new OrderManagement.InventoryService.Models.Product
//            {
//                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
//                Name = "iPhone 15 Pro",
//                Description = "Apple iPhone 15 Pro 128GB",
//                Price = 999.99m,
//                StockQuantity = 50,
//                IsActive = true
//            },
//            new OrderManagement.InventoryService.Models.Product
//            {
//                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
//                Name = "Samsung Galaxy S24",
//                Description = "Samsung Galaxy S24 Ultra 256GB",
//                Price = 1199.99m,
//                StockQuantity = 30,
//                IsActive = true
//            },
//            new OrderManagement.InventoryService.Models.Product
//            {
//                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
//                Name = "MacBook Pro M3",
//                Description = "MacBook Pro 14\" M3 512GB",
//                Price = 1999.99m,
//                StockQuantity = 20,
//                IsActive = true
//            },
//            new OrderManagement.InventoryService.Models.Product
//            {
//                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
//                Name = "AirPods Pro",
//                Description = "Apple AirPods Pro 2nd Gen",
//                Price = 249.99m,
//                StockQuantity = 100,
//                IsActive = true
//            },
//            new OrderManagement.InventoryService.Models.Product
//            {
//                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
//                Name = "Dell XPS 13",
//                Description = "Dell XPS 13 Laptop Intel i7",
//                Price = 1299.99m,
//                StockQuantity = 15,
//                IsActive = true
//            }
//        };

//        await context.Products.AddRangeAsync(products);
//        await context.SaveChangesAsync();

//        Log.Information("Initial product data seeded successfully");
//    }
//}
