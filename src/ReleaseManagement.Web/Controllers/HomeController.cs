using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Approvals;
using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Web.Models;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly IReleaseAppService _releaseAppService;
    private readonly IApprovalService _approvalService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public HomeController(
        IReleaseAppService releaseAppService,
        IApprovalService approvalService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _releaseAppService = releaseAppService;
        _approvalService = approvalService;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var releases = await _releaseAppService.GetMyReleasesAsync(cancellationToken);
        var pending = await _approvalService.GetPendingAsync(cancellationToken);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var deployedThisMonth = await _dbContext.Releases.AsNoTracking()
            .CountAsync(
                release => release.CurrentStatus == ReleaseStatus.Deployed &&
                           release.ActualReleaseDate >= startOfMonth,
                cancellationToken);

        var model = new DashboardViewModel
        {
            MyOpenReleases = releases.Count(item =>
                item.CurrentStatus is not (
                    ReleaseStatus.Closed or
                    ReleaseStatus.Cancelled or
                    ReleaseStatus.Rejected or
                    ReleaseStatus.RolledBack)),
            ActionRequired = releases.Count(item => item.ActionRequiredFromCurrentUser),
            PendingApprovals = pending.Count,
            ReadyForRelease = releases.Count(item => item.CurrentStatus == ReleaseStatus.ReadyForRelease),
            DeployedThisMonth = deployedThisMonth,
            RecentReleases = releases.Take(8).Select(MapListItem).ToArray()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static ReleaseListItemViewModel MapListItem(Application.DTOs.Releases.ReleaseListItemDto item) =>
        new()
        {
            Id = item.Id,
            ReleaseNumber = item.ReleaseNumber,
            Title = item.Title,
            ProductName = item.ProductName,
            ReleaseVersion = item.ReleaseVersion,
            PlannedReleaseDate = item.PlannedReleaseDate,
            CurrentStatus = item.CurrentStatus,
            CurrentStatusDisplay = item.CurrentStatusDisplay,
            CurrentResponsibleDisplay = item.CurrentResponsibleDisplay,
            UpdatedDate = item.UpdatedDate,
            ActionRequiredFromCurrentUser = item.ActionRequiredFromCurrentUser
        };
}
