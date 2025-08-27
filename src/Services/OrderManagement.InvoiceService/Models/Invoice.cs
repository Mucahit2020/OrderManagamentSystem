using OrderManagement.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.InvoiceService.Models;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Required]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    
    public string? ExternalInvoiceId { get; set; }
    
    public string? ExternalReference { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? FailureReason { get; set; }
    
    public List<InvoiceItem> Items { get; set; } = new();
}