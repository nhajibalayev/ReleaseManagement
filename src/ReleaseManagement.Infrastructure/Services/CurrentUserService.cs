using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Constants;

namespace ReleaseManagement.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string UserName =>
        User?.Identity?.Name ??
        User?.FindFirstValue(ClaimTypes.Name) ??
        string.Empty;

    public string Email =>
        User?.FindFirstValue(ClaimTypes.Email) ??
        string.Empty;

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        ?? [];

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        if (Roles.Any(item => string.Equals(item, RoleNames.Administrator, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return Roles.Any(item => string.Equals(item, role, StringComparison.OrdinalIgnoreCase));
    }
}
