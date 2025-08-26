using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Common.Events;

public abstract record BaseEvent
{
    [Required]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [Required]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    [Required]
    public string EventType { get; init; } = string.Empty;

    [Required]
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
}