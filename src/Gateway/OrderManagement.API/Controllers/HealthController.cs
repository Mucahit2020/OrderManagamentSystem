using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Service = "Order Management API Gateway",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}