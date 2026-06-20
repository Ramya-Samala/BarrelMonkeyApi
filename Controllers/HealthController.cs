using Microsoft.AspNetCore.Mvc;

namespace BarrelMonkeyApi.Controllers;

// Simple health check endpoint. No auth required — load balancers and uptime monitors
// need to hit this without credentials.
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private static readonly DateTime _startedAt = DateTime.UtcNow;

    // Returns the current health status of the service.
    // If this responds with 200, we're alive.
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "BarrelMonkeyApi",
            startedAt = _startedAt,
            uptimeSeconds = (DateTime.UtcNow - _startedAt).TotalSeconds,
            timestamp = DateTime.UtcNow,
        });
    }
}
