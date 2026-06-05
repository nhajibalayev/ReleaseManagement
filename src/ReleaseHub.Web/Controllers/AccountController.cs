using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Application.Options;
using ReleaseHub.Web.Models;

namespace ReleaseHub.Web.Controllers;

[AllowAnonymous]
public sealed class AccountController : Controller
{
    private readonly ISsoClient _ssoClient;
    private readonly SsoOptions _ssoOptions;

    public AccountController(ISsoClient ssoClient, IOptions<SsoOptions> ssoOptions)
    {
        _ssoClient = ssoClient;
        _ssoOptions = ssoOptions.Value;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { SsoLoginUrl = _ssoOptions.LoginUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginFormModel form, string? returnUrl = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(form.AccessToken))
        {
            ModelState.AddModelError(nameof(form.AccessToken), "Access token is required.");
            return View(new LoginViewModel { SsoLoginUrl = _ssoOptions.LoginUrl });
        }

        var user = await _ssoClient.ValidateAsync(form.AccessToken.Trim(), ct);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "SSO token validation failed.");
            return View(new LoginViewModel { SsoLoginUrl = _ssoOptions.LoginUrl });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, string.IsNullOrEmpty(user.DisplayName) ? user.UserId : user.DisplayName)
        };
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "ReleaseTasks");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
