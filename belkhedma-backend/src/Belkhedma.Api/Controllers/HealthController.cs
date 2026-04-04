using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            app = "Belkhedma.Backend",
            status = "Healthy",
            utc = DateTime.UtcNow
        });
    }
}
