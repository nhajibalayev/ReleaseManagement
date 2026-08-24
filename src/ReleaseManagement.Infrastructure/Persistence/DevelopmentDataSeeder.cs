using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Infrastructure.Identity;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly SeedOptions _options;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        ApplicationDbContext dbContext,
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<SeedOptions> options,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }

        foreach (var roleName in RoleNames.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }

            if (!await _dbContext.AppRoles.AnyAsync(role => role.Name == roleName, cancellationToken))
            {
                _dbContext.AppRoles.Add(new Role(Guid.NewGuid(), roleName, $"{roleName} role"));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var environments = new[]
        {
            ("Development", "DEV", false),
            ("Test", "TEST", false),
            ("PreProduction", "PREPROD", false),
            ("Production", "PROD", true)
        };

        foreach (var (name, code, isProduction) in environments)
        {
            if (!await _dbContext.Environments.AnyAsync(item => item.Code == code, cancellationToken))
            {
                _dbContext.Environments.Add(
                    new DeploymentEnvironment(Guid.NewGuid(), name, code, name, isProduction));
            }
        }

        var productId = await EnsureProductAsync(cancellationToken);
        await EnsureServiceAsync(productId, cancellationToken);

        var users = new (string UserName, string FullName, string Email, string Role)[]
        {
            ("admin", "System Administrator", "admin@local.test", RoleNames.Administrator),
            ("po", "Product Owner", "po@local.test", RoleNames.ProductOwner),
            ("rm", "Release Manager", "rm@local.test", RoleNames.ReleaseManager),
            ("pentest", "Pentest User", "pentest@local.test", RoleNames.Pentest),
            ("infosec", "InfoSec User", "infosec@local.test", RoleNames.InfoSec),
            ("business", "Business Approver", "business@local.test", RoleNames.BusinessApprover),
            ("devops", "DevOps User", "devops@local.test", RoleNames.DevOps),
            ("auditor", "Auditor", "auditor@local.test", RoleNames.Auditor)
        };

        foreach (var user in users)
        {
            var identityUser = await EnsureIdentityUserAsync(user, cancellationToken);
            await EnsureApplicationUserAsync(identityUser, cancellationToken);

            if (user.Role == RoleNames.ProductOwner)
            {
                var exists = await _dbContext.UserProductAccesses.AnyAsync(
                    access => access.UserId == identityUser.Id && access.ProductId == productId,
                    cancellationToken);

                if (!exists)
                {
                    _dbContext.UserProductAccesses.Add(
                        new UserProductAccess(
                            identityUser.Id,
                            productId,
                            ProductAccessType.CreateRelease));
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Development seed data applied.");
    }

    private async Task<Guid> EnsureProductAsync(CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.Code == "PAY", cancellationToken);

        if (product is not null)
        {
            return product.Id;
        }

        product = new Product(
            Guid.NewGuid(),
            "Payments Platform",
            "PAY",
            "Sample product for local development",
            DateTime.UtcNow);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    private async Task EnsureServiceAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (await _dbContext.Services.AnyAsync(
                item => item.ProductId == productId && item.Code == "PAY-API",
                cancellationToken))
        {
            return;
        }

        _dbContext.Services.Add(
            new Service(
                Guid.NewGuid(),
                productId,
                "Payments API",
                "PAY-API",
                "https://dev.azure.com/example/payments",
                "Payments",
                null));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AppIdentityUser> EnsureIdentityUserAsync(
        (string UserName, string FullName, string Email, string Role) user,
        CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByNameAsync(user.UserName);
        if (existing is null)
        {
            existing = new AppIdentityUser
            {
                Id = Guid.NewGuid(),
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = true,
                FullName = user.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(existing, _options.DefaultPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await _userManager.IsInRoleAsync(existing, user.Role))
        {
            await _userManager.AddToRoleAsync(existing, user.Role);
        }

        return existing;
    }

    private async Task EnsureApplicationUserAsync(
        AppIdentityUser identityUser,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ApplicationUsers.AnyAsync(item => item.Id == identityUser.Id, cancellationToken);
        if (exists)
        {
            return;
        }

        var applicationUser = new ApplicationUser(
            identityUser.Id,
            identityUser.UserName!,
            identityUser.FullName,
            identityUser.Email!,
            DateTime.UtcNow);

        _dbContext.ApplicationUsers.Add(applicationUser);
    }
}
