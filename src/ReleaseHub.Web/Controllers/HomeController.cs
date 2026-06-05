using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ReleaseHub.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "ReleaseTasks");

    [AllowAnonymous]
    public IActionResult Error() => View();
}
