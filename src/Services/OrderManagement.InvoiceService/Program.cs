using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderManagement.Common.Configuration;
using OrderManagement.InvoiceService.Data;
using OrderManagement.InvoiceService.EventHandlers;
using OrderManagement.InvoiceService.ExternalServices;
using OrderManagement.InvoiceService.Interfaces;
using OrderManagement.InvoiceService.Mappings;
using OrderManagement.InvoiceService.Repositories;
using OrderManagement.InvoiceService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/invoice-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice Service API",
        Version = "v1",
        Description = "Invoice management microservice for Order Management System"
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
builder.Services.AddDbContext<InvoiceContext>(options =>
{
    options.UseNpgsql(databaseConfig?.ConnectionString ??
        builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .LogTo(Console.WriteLine, LogLevel.Information);
});

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(InvoiceMappingProfile));

// Repository Registration
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

// Service Registration
builder.Services.AddScoped<IInvoiceService, OrderManagement.InvoiceService.Services.InvoiceService>();
builder.Services.AddScoped<IExternalInvoiceService, MockExternalInvoiceService>();

// MassTransit Configuration
var rabbitMqConfig = builder.Configuration.GetSection(RabbitMqConfig.SectionName).Get<RabbitMqConfig>();

builder.Services.AddMassTransit(x =>
{
    // Add consumers (event handlers)
    x.AddConsumer<OrderCompletedHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqConfig?.Host ?? "rabbitmq", rabbitMqConfig?.VirtualHost ?? "/", h =>
        {
            h.Username(rabbitMqConfig?.Username ?? "admin");
            h.Password(rabbitMqConfig?.Password ?? "password123");
        });

        // Configure endpoints with naming conventions
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("invoice", false));

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

// Database migration on startup
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<InvoiceContext>();
//    try
//    {
//        await context.Database.MigrateAsync();
//        Log.Information("Database migration completed successfully");
//    }
//    catch (Exception ex)
//    {
//        Log.Fatal(ex, "Database migration failed");
//        throw;
//    }
//}

Log.Information("Invoice Service starting up...");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Invoice Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
