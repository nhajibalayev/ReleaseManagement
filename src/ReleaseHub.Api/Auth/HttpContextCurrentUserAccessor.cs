using System.Security.Claims;
using ReleaseHub.Application.Abstractions.Auth;

namespace ReleaseHub.Api.Auth;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public CurrentUser? User
    {
        get
        {
            var principal = _accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;

            return new CurrentUser
            {
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                DisplayName = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
            };
        }
    }
}
