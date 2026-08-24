using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IApplicationDbContext _dbContext;

    public ReportsController(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var byStatus = await _dbContext.Releases.AsNoTracking()
            .GroupBy(item => item.CurrentStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byRisk = await _dbContext.Releases.AsNoTracking()
            .GroupBy(item => item.RiskLevel)
            .Select(group => new { Risk = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var failed = await _dbContext.Releases.AsNoTracking()
            .CountAsync(
                item => item.CurrentStatus == ReleaseStatus.DeploymentFailed ||
                        item.CurrentStatus == ReleaseStatus.RolledBack,
                cancellationToken);

        var deployed = await _dbContext.Releases.AsNoTracking()
            .CountAsync(
                item => item.CurrentStatus == ReleaseStatus.Deployed ||
                        item.CurrentStatus == ReleaseStatus.Closed,
                cancellationToken);

        ViewBag.ByStatus = byStatus;
        ViewBag.ByRisk = byRisk;
        ViewBag.Failed = failed;
        ViewBag.Deployed = deployed;
        return View();
    }
}
