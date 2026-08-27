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
}

public sealed class ExternalUserProvisioner : IExternalUserProvisioner
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;
    private readonly AzureAdOptions _options;
    private readonly ILogger<ExternalUserProvisioner> _logger;

    public ExternalUserProvisioner(
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext,
        IClock clock,
        IOptions<AzureAdOptions> options,
        ILogger<ExternalUserProvisioner> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppIdentityUser> ProvisionFromClaimsAsync(
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

        var existing = await _userManager.Users
            .FirstOrDefaultAsync(user => user.ExternalId == objectId, cancellationToken);

        if (existing is null)
        {
            existing = await _userManager.FindByEmailAsync(email);
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
                ExternalId = objectId,
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

            var defaultRole = string.IsNullOrWhiteSpace(_options.DefaultRole)
                ? RoleNames.ProductOwner
                : _options.DefaultRole;

            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(defaultRole));
            }

            if (!await _userManager.IsInRoleAsync(existing, defaultRole))
            {
                await _userManager.AddToRoleAsync(existing, defaultRole);
            }

            _logger.LogInformation(
                "Provisioned SSO user {UserName} ({ExternalId}) with role {Role}",
                existing.UserName,
                objectId,
                defaultRole);
        }
        else
        {
            existing.ExternalId ??= objectId;
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
            applicationUser.SetExternalIdentity(objectId, now);
            _dbContext.ApplicationUsers.Add(applicationUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(applicationUser.ExternalId))
        {
            applicationUser.SetExternalIdentity(objectId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }
}
