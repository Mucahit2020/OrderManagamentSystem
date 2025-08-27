using Microsoft.EntityFrameworkCore;
using OrderManagement.InvoiceService.Models;

namespace OrderManagement.InvoiceService.Data;

public class InvoiceContext : DbContext
{
    public InvoiceContext(DbContextOptions<InvoiceContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.ProcessedAt).HasColumnType("timestamptz");

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.ProductId);
        });
    }
}