using System.ComponentModel.DataAnnotations;

namespace OrderManagement.InvoiceService.Models;

public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid InvoiceId { get; set; }
    
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}