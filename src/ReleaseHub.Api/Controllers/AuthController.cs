using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Application.Options;

namespace ReleaseHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly SsoOptions _ssoOptions;

    public AuthController(ICurrentUserAccessor currentUser, IOptions<SsoOptions> ssoOptions)
    {
        _currentUser = currentUser;
        _ssoOptions = ssoOptions.Value;
    }

    [HttpGet("sso-config")]
    [AllowAnonymous]
    public ActionResult<SsoConfigResponse> GetSsoConfig() =>
        new SsoConfigResponse(_ssoOptions.LoginUrl);

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserResponse> Me()
    {
        var u = _currentUser.User;
        if (u is null) return Unauthorized();
        return new CurrentUserResponse(u.UserId, u.Email, u.DisplayName, u.Roles.ToArray());
    }

    public sealed record SsoConfigResponse(string LoginUrl);
    public sealed record CurrentUserResponse(string UserId, string Email, string DisplayName, IReadOnlyCollection<string> Roles);
}
