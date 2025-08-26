using Microsoft.EntityFrameworkCore;
using OrderManagement.OrderService.Models;

namespace OrderManagement.OrderService.Data;

public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(e => e.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("idx_orders_idempotency_key");

            entity.HasIndex(e => e.CustomerId)
                .HasDatabaseName("idx_orders_customer_id");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_orders_status");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamptz");

            // Relationships
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.OrderId)
                .HasDatabaseName("idx_order_items_order_id");

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("idx_order_items_product_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamptz");
        });
    }
}