using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Application.Planning;
using ReleaseManagement.Application.Releases;
using ReleaseManagement.Web.ViewModels;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class DeploymentsController : Controller
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IReleaseWorkflowService _workflow;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IPlanningService _planning;

    public DeploymentsController(
        IApplicationDbContext dbContext,
        IReleaseWorkflowService workflow,
        ICurrentUserService currentUser,
        IClock clock,
        IPlanningService planning)
    {
        _dbContext = dbContext;
        _workflow = workflow;
        _currentUser = currentUser;
        _clock = clock;
        _planning = planning;
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddMonths(-1);

        var releases = await _dbContext.Releases.AsNoTracking()
            .Include(item => item.Services)
            .Where(item => item.PlannedWindowEnd >= from)
            .OrderBy(item => item.PlannedWindowStart)
            .Take(300)
            .ToListAsync(cancellationToken);

        var products = await _dbContext.Products.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var environments = await _dbContext.Environments.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var serviceNames = await _dbContext.Services.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        var managerIds = releases
            .Where(item => item.ReleaseManagerUserId.HasValue)
            .Select(item => item.ReleaseManagerUserId!.Value)
            .Distinct()
            .ToArray();
        var managers = await _dbContext.Users.AsNoTracking()
            .Where(user => managerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        var freezes = await _planning.GetFreezePeriodsAsync(includeInactive: false, cancellationToken);
        var freezeRows = freezes
            .Where(item => item.EndDate >= from)
            .Select(item => new FreezeConflictRowViewModel
            {
                FreezePeriodId = item.Id,
                Name = item.Name,
                FreezeType = item.FreezeType.ToString(),
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Authority = item.Authority
            })
            .ToArray();

        var rows = releases
            .Select(item => new CalendarRowViewModel
            {
                Id = item.Id,
                ReleaseNumber = item.ReleaseNumber,
                Title = item.Title,
                Track = item.Track.ToString(),
                Category = item.Category,
                ExecutionMode = item.ExecutionMode,
                ProductName = products.GetValueOrDefault(item.ProductId),
                EnvironmentName = environments.GetValueOrDefault(item.EnvironmentId),
                Services = string.Join(", ", item.Services.Select(service => serviceNames.GetValueOrDefault(service.ServiceId, "?"))),
                WindowStart = item.PlannedWindowStart,
                WindowEnd = item.PlannedWindowEnd,
                ReleaseManagerName = item.ReleaseManagerUserId is { } managerId ? managers.GetValueOrDefault(managerId) : null,
                DowntimeRequired = item.DowntimeRequired,
                ExpectedDowntimeMinutes = item.ExpectedDowntimeMinutes,
                PlannedMaintenance = item.PlannedMaintenance,
                KeyDependencies = item.KeyDependencies,
                CurrentStatus = item.CurrentStatus,
                CurrentStatusDisplay = ReleaseStatusDisplay.Format(item.CurrentStatus),
                InFreeze = freezeRows.Any(freeze => freeze.StartDate < item.PlannedWindowEnd && freeze.EndDate > item.PlannedWindowStart)
            })
            .ToArray();

        return View(new CalendarPageViewModel { Releases = rows, Freezes = freezeRows });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanDeployRelease)]
    public async Task<IActionResult> Start(Guid id, string? pipelineUrl, string? buildNumber, CancellationToken cancellationToken)
    {
        try
        {
            var environmentId = await _dbContext.Releases.AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => item.EnvironmentId)
                .SingleAsync(cancellationToken);

            await _workflow.TransitionAsync(
                new TransitionReleaseRequest
                {
                    ReleaseId = id,
                    TargetStatus = ReleaseStatus.DeploymentInProgress,
                    Comment = "Deployment started."
                },
                cancellationToken);

            _dbContext.DeploymentRecords.Add(new DeploymentRecord(
                Guid.NewGuid(),
                id,
                environmentId,
                _currentUser.UserId,
                _clock.UtcNow,
                pipelineUrl,
                null,
                buildNumber));

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Deployment started.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Releases", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanDeployRelease)]
    public async Task<IActionResult> Complete(Guid id, string? comment, CancellationToken cancellationToken)
    {
        try
        {
            await _workflow.TransitionAsync(
                new TransitionReleaseRequest
                {
                    ReleaseId = id,
                    TargetStatus = ReleaseStatus.Deployed,
                    Comment = comment
                },
                cancellationToken);

            var record = await _dbContext.DeploymentRecords
                .Where(item => item.ReleaseId == id)
                .OrderByDescending(item => item.StartedDate)
                .FirstOrDefaultAsync(cancellationToken);

            record?.Complete(_currentUser.UserId, _clock.UtcNow, comment);
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Deployment marked as successful.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Releases", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanDeployRelease)]
    public async Task<IActionResult> Fail(Guid id, string errorDetails, bool rollbackRequired, CancellationToken cancellationToken)
    {
        try
        {
            await _workflow.TransitionAsync(
                new TransitionReleaseRequest
                {
                    ReleaseId = id,
                    TargetStatus = ReleaseStatus.DeploymentFailed,
                    Comment = errorDetails
                },
                cancellationToken);

            var record = await _dbContext.DeploymentRecords
                .Where(item => item.ReleaseId == id)
                .OrderByDescending(item => item.StartedDate)
                .FirstOrDefaultAsync(cancellationToken);

            record?.Fail(_currentUser.UserId, _clock.UtcNow, errorDetails, rollbackRequired);
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Deployment failure recorded.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Releases", new { id });
    }
}
