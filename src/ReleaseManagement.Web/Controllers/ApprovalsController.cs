using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Approvals;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Approvals;
using ReleaseManagement.Application.Readiness;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class ApprovalsController : Controller
{
    private readonly IApprovalService _approvalService;
    private readonly IReadinessService _readiness;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ApprovalsController(
        IApprovalService approvalService,
        IReadinessService readiness,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _approvalService = approvalService;
        _readiness = readiness;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var legacy = await _approvalService.GetPendingAsync(cancellationToken);
        var readinessIds = await _readiness.GetReleasesAwaitingMyReadinessAsync(cancellationToken);

        var readinessRows = new List<ReadinessQueueRowViewModel>();
        if (readinessIds.Count > 0)
        {
            var releases = await _dbContext.Releases.AsNoTracking()
                .Include(item => item.ReadinessControls)
                .Where(item => readinessIds.Contains(item.Id))
                .OrderBy(item => item.PlannedWindowStart)
                .ToListAsync(cancellationToken);

            var productIds = releases.Select(item => item.ProductId).Distinct().ToArray();
            var products = await _dbContext.Products.AsNoTracking()
                .Where(item => productIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

            var isAdmin = _currentUser.IsInRole(RoleNames.Administrator);

            readinessRows.AddRange(releases.Select(release => new ReadinessQueueRowViewModel
            {
                ReleaseId = release.Id,
                ReleaseNumber = release.ReleaseNumber,
                Title = release.Title,
                ProductName = products.GetValueOrDefault(release.ProductId),
                Category = release.Category,
                ExecutionMode = release.ExecutionMode,
                PlannedWindowStart = release.PlannedWindowStart,
                PendingControls = string.Join(", ", release.ReadinessControls
                    .Where(control => control.IsRequired && control.Status == ReadinessControlStatus.Pending)
                    .Select(control => ReleaseReadinessRules.GetDefinition(control.ControlType))
                    .Where(definition => isAdmin || ReleaseReadinessRules.CanUserSetControl(definition, _currentUser.Roles))
                    .Select(definition => definition.Title))
            }));
        }

        var model = new PendingWorkPageViewModel
        {
            Readiness = readinessRows,
            Legacy = legacy.Select(item => new PendingApprovalRowViewModel
            {
                ReleaseId = item.ReleaseId,
                ReleaseNumber = item.ReleaseNumber,
                Title = item.Title,
                ProductName = item.ProductName,
                RequestedDate = item.RequestedDate,
                PlannedReleaseDate = item.PlannedReleaseDate,
                ApprovalType = item.ApprovalType,
                SlaDueDate = item.SlaDueDate
            }).ToArray()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ApprovalDecisionFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _approvalService.DecideAsync(
                new ApprovalDecisionRequest
                {
                    ReleaseId = model.ReleaseId,
                    ApprovalType = model.ApprovalType,
                    Decision = model.Decision,
                    Comment = model.Comment
                },
                cancellationToken);

            TempData["Success"] = $"Decision recorded: {model.Decision}.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException or FluentValidation.ValidationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Releases", new { id = model.ReleaseId });
    }
}
