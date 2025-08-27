using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Service = "Invoice Service",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}