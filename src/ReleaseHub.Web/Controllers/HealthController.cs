using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ReleaseHub.Web.Controllers;

[AllowAnonymous]
[Route("healthz")]
public sealed class HealthController : Controller
{
    [HttpGet("")]
    public IActionResult Get() => Json(new { status = "ok" });
}
