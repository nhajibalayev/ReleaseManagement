using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Identity;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

public sealed class AccountController : Controller
{
    public const string AzureAdScheme = "AzureAd";

    private readonly SignInManager<AppIdentityUser> _signInManager;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IExternalUserProvisioner _externalUserProvisioner;
    private readonly AzureAdOptions _azureAd;

    public AccountController(
        SignInManager<AppIdentityUser> signInManager,
        UserManager<AppIdentityUser> userManager,
        IExternalUserProvisioner externalUserProvisioner,
        IOptions<AzureAdOptions> azureAd)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _externalUserProvisioner = externalUserProvisioner;
        _azureAd = azureAd.Value;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            AzureAdEnabled = _azureAd.Enabled,
            AllowLocalLogin = !_azureAd.Enabled || _azureAd.AllowLocalLogin
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.AzureAdEnabled = _azureAd.Enabled;
        model.AllowLocalLogin = !_azureAd.Enabled || _azureAd.AllowLocalLogin;

        if (_azureAd.Enabled && !_azureAd.AllowLocalLogin)
        {
            ModelState.AddModelError(string.Empty, "Local login is disabled. Use Microsoft SSO.");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string? returnUrl = null)
    {
        if (!_azureAd.Enabled)
        {
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            AzureAdScheme,
            redirectUrl);
        return Challenge(properties, AzureAdScheme);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(
        string? returnUrl = null,
        string? remoteError = null,
        CancellationToken cancellationToken = default)
    {
        if (!_azureAd.Enabled)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            TempData["Error"] = $"SSO error: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["Error"] = "SSO login failed. External login info was not found.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _externalUserProvisioner.ProvisionFromClaimsAsync(
            info.Principal,
            cancellationToken);

        if (!user.IsActive)
        {
            TempData["Error"] = "Your account is deactivated.";
            return RedirectToAction(nameof(Login));
        }

            var loginResult = await _userManager.AddLoginAsync(user, info);
            if (!loginResult.Succeeded &&
                !loginResult.Errors.Any(error =>
                    error.Code.Contains("LoginAlreadyAssociated", StringComparison.OrdinalIgnoreCase) ||
                    error.Description.Contains("already", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = string.Join("; ", loginResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Login));
            }

        var extraClaims = new List<System.Security.Claims.Claim>();
        var oid = info.Principal.FindFirstValue("oid");
        var tid = info.Principal.FindFirstValue("tid");
        if (!string.IsNullOrWhiteSpace(oid))
        {
            extraClaims.Add(new System.Security.Claims.Claim("oid", oid));
        }

        if (!string.IsNullOrWhiteSpace(tid))
        {
            extraClaims.Add(new System.Security.Claims.Claim("tid", tid));
        }

        await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, extraClaims);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        if (_azureAd.Enabled)
        {
            return SignOut(
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(Login), "Account")
                },
                IdentityConstants.ApplicationScheme,
                AzureAdScheme);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
