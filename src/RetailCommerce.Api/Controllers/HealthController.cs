using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RetailCommerce.Api.Controllers;

/// <summary>Deliberately trivial and unauthenticated — ConnectivityService polls this to tell
/// "the internet is up but our server specifically is down" apart from a genuine outage, which a
/// browser online/offline event alone can't distinguish.</summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", serverTimeUtc = DateTimeOffset.UtcNow });
}
