using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Identity;

public interface IExternalUserProvisioner
{
    Task<AppIdentityUser> ProvisionFromClaimsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<AppIdentityUser> ProvisionFromWindowsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<AppIdentityUser> ProvisionFromActiveDirectoryAsync(
        ActiveDirectoryIdentity identity,
        CancellationToken cancellationToken = default);
}

public sealed class ExternalUserProvisioner : IExternalUserProvisioner
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;
    private readonly AzureAdOptions _azureAd;
    private readonly WindowsAuthOptions _windowsAuth;
    private readonly ILogger<ExternalUserProvisioner> _logger;

    public ExternalUserProvisioner(
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext,
        IClock clock,
        IOptions<AzureAdOptions> azureAd,
        IOptions<WindowsAuthOptions> windowsAuth,
        ILogger<ExternalUserProvisioner> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _clock = clock;
        _azureAd = azureAd.Value;
        _windowsAuth = windowsAuth.Value;
        _logger = logger;
    }

    public Task<AppIdentityUser> ProvisionFromClaimsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var objectId = principal.FindFirstValue("oid")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Azure AD object id claim is missing.");

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue("upn")
            ?? $"{objectId}@sso.local";

        var displayName = principal.FindFirstValue("name")
            ?? principal.Identity?.Name
            ?? email;

        var userName = email.Contains('@', StringComparison.Ordinal)
            ? email[..email.IndexOf('@', StringComparison.Ordinal)]
            : email;

        return UpsertExternalUserAsync(
            externalId: objectId,
            userName: userName,
            email: email,
            displayName: displayName,
            defaultRole: _azureAd.DefaultRole,
            cancellationToken);
    }

    public Task<AppIdentityUser> ProvisionFromWindowsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var identityName = principal.Identity?.Name
            ?? throw new InvalidOperationException("Windows identity name is missing.");

        var sid = principal.FindFirstValue(ClaimTypes.PrimarySid)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? identityName;

        var shortName = identityName.Contains('\\', StringComparison.Ordinal)
            ? identityName[(identityName.LastIndexOf('\\') + 1)..]
            : identityName;

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? $"{shortName.Replace(' ', '.')}@ad.local";

        return UpsertExternalUserAsync(
            externalId: $"win:{sid}",
            userName: shortName,
            email: email,
            displayName: identityName,
            defaultRole: _windowsAuth.DefaultRole,
            cancellationToken);
    }

    public Task<AppIdentityUser> ProvisionFromActiveDirectoryAsync(
        ActiveDirectoryIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var email = string.IsNullOrWhiteSpace(identity.Email)
            ? $"{identity.UserName.Replace(' ', '.')}@ad.local"
            : identity.Email;

        return UpsertExternalUserAsync(
            externalId: identity.ExternalId,
            userName: identity.UserName,
            email: email,
            displayName: identity.DisplayName,
            defaultRole: _windowsAuth.DefaultRole,
            cancellationToken);
    }

    private async Task<AppIdentityUser> UpsertExternalUserAsync(
        string externalId,
        string userName,
        string email,
        string displayName,
        string defaultRole,
        CancellationToken cancellationToken)
    {
        var existing = await _userManager.Users
            .FirstOrDefaultAsync(user => user.ExternalId == externalId, cancellationToken);

        if (existing is null)
        {
            existing = await _userManager.FindByNameAsync(userName)
                ?? await _userManager.FindByEmailAsync(email);
        }

        var now = _clock.UtcNow;

        if (existing is null)
        {
            existing = new AppIdentityUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                ExternalId = externalId,
                FullName = displayName,
                IsActive = true,
                CreatedDate = now,
                UpdatedDate = now
            };

            var createResult = await _userManager.CreateAsync(existing);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }

            var role = string.IsNullOrWhiteSpace(defaultRole)
                ? RoleNames.ProductOwner
                : defaultRole;

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }

            if (!await _userManager.IsInRoleAsync(existing, role))
            {
                await _userManager.AddToRoleAsync(existing, role);
            }

            _logger.LogInformation(
                "Provisioned external user {UserName} ({ExternalId}) with role {Role}",
                existing.UserName,
                externalId,
                role);
        }
        else
        {
            existing.ExternalId ??= externalId;
            existing.FullName = displayName;
            existing.Email = email;
            existing.UpdatedDate = now;
            await _userManager.UpdateAsync(existing);
        }

        var applicationUser = await _dbContext.ApplicationUsers
            .SingleOrDefaultAsync(user => user.Id == existing.Id, cancellationToken);

        if (applicationUser is null)
        {
            applicationUser = new ApplicationUser(
                existing.Id,
                existing.UserName!,
                existing.FullName,
                existing.Email!,
                now);
            applicationUser.SetExternalIdentity(externalId, now);
            _dbContext.ApplicationUsers.Add(applicationUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(applicationUser.ExternalId))
        {
            applicationUser.SetExternalIdentity(externalId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }
}
