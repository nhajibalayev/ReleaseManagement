using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Domain.Constants;
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
    private static readonly string[] RoleOrder =
    [
        RoleNames.Administrator,
        RoleNames.ReleaseManager,
        RoleNames.TechnicalOwner,
        RoleNames.ProductOwner,
        RoleNames.QA,
        RoleNames.InfoSec,
        RoleNames.Risk,
        RoleNames.ChapterLead,
        RoleNames.DBA,
        RoleNames.ITOperations,
        RoleNames.DevOps,
        RoleNames.Auditor,
        RoleNames.Pentest,
        RoleNames.BusinessApprover
    ];

    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UsersController(
        UserManager<AppIdentityUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(user => user.UserName).ToList();
        var rows = new List<UserRowViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserRowViewModel(
                user.Id,
                user.UserName ?? string.Empty,
                user.FullName,
                user.Email,
                user.IsActive,
                SortRoles(roles)));
        }

        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return View(new UserEditViewModel(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            user.Email,
            user.IsActive,
            RoleOrder,
            roles.ToHashSet(StringComparer.OrdinalIgnoreCase),
            user.Id == _currentUser.UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string[]? roles, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var requested = (roles ?? [])
            .Where(RoleNames.All.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var current = (await _userManager.GetRolesAsync(user)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isSelf = user.Id == _currentUser.UserId;

        if (isSelf && !requested.Contains(RoleNames.Administrator))
        {
            TempData["Error"] = "You cannot remove the Administrator role from your own account.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (isSelf && !isActive)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        foreach (var role in requested)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToArray();

        if (toAdd.Length > 0)
        {
            var result = await _userManager.AddToRolesAsync(user, toAdd);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        if (toRemove.Length > 0)
        {
            var result = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        var wasActive = user.IsActive;
        if (wasActive != isActive)
        {
            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        if (toAdd.Length > 0 || toRemove.Length > 0 || wasActive != isActive)
        {
            await _audit.WriteAsync(
                "UserRolesChanged",
                nameof(AppIdentityUser),
                user.Id.ToString(),
                new { Roles = SortRoles(current), IsActive = wasActive },
                new { Roles = SortRoles(requested), IsActive = isActive },
                cancellationToken);
            TempData["Success"] = $"Roles for {user.UserName} updated.";
        }
        else
        {
            TempData["Success"] = "No changes.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static IReadOnlyList<string> SortRoles(IEnumerable<string> roles)
    {
        var set = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ordered = RoleOrder.Where(set.Contains).ToList();
        ordered.AddRange(set.Where(role => !RoleOrder.Contains(role, StringComparer.OrdinalIgnoreCase)).OrderBy(role => role));
        return ordered;
    }
}

public sealed record UserRowViewModel(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record UserEditViewModel(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> AllRoles,
    IReadOnlySet<string> AssignedRoles,
    bool IsSelf);
