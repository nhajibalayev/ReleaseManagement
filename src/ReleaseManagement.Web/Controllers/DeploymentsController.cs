using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Application.Releases;
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

    public DeploymentsController(
        IApplicationDbContext dbContext,
        IReleaseWorkflowService workflow,
        ICurrentUserService currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _workflow = workflow;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(CancellationToken cancellationToken)
    {
        var releases = await _dbContext.Releases.AsNoTracking()
            .Where(item => item.PlannedReleaseDate >= DateTime.UtcNow.Date.AddMonths(-1))
            .OrderBy(item => item.PlannedReleaseDate)
            .Take(200)
            .Select(item => new
            {
                item.Id,
                item.ReleaseNumber,
                item.Title,
                item.PlannedReleaseDate,
                item.CurrentStatus,
                item.ProductId,
                item.EnvironmentId
            })
            .ToListAsync(cancellationToken);

        var products = await _dbContext.Products.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var environments = await _dbContext.Environments.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        ViewBag.Products = products;
        ViewBag.Environments = environments;
        return View(releases);
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
