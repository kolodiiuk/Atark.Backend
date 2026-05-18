using Microsoft.AspNetCore.Mvc;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("/health")]
public class HealthController : BaseController<HealthController>
{
    public HealthController(ILogger<HealthController> logger) : base(logger)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Health()
    {
        return Ok(new { status = "healthy" });
    }
}
