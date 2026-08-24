using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Identity;

namespace ReleaseManagement.Web.Controllers.Admin;

[Authorize(Policy = AuthorizationPolicies.CanManageSystem)]
public sealed class ProductsController : Controller
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IClock _clock;

    public ProductsController(IApplicationDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Products.AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string code, string? description, CancellationToken cancellationToken)
    {
        _dbContext.Products.Add(new Product(Guid.NewGuid(), name, code, description, _clock.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Policy = AuthorizationPolicies.CanManageSystem)]
public sealed class EnvironmentsController : Controller
{
    private readonly IApplicationDbContext _dbContext;

    public EnvironmentsController(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Environments.AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return View(items);
    }
}

[Authorize(Policy = AuthorizationPolicies.CanManageSystem)]
public sealed class UsersController : Controller
{
    private readonly UserManager<AppIdentityUser> _userManager;

    public UsersController(UserManager<AppIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(user => user.UserName).ToList();
        var rows = new List<object>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.IsActive,
                Roles = string.Join(", ", roles)
            });
        }

        return View(rows);
    }
}
