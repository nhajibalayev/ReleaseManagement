using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Application.Governance;
using ReleaseManagement.Application.Planning;
using ReleaseManagement.Application.PostRelease;
using ReleaseManagement.Application.Readiness;
using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Infrastructure.Identity;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class ReleasesController : Controller
{
    private const int WizardSteps = 6;

    private readonly IReleaseAppService _releaseAppService;
    private readonly IReleaseWorkflowService _workflowService;
    private readonly IReadinessService _readiness;
    private readonly IPostReleaseService _postRelease;
    private readonly IPlanningService _planning;
    private readonly IGovernanceService _governance;
    private readonly IApplicationDbContext _dbContext;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditService _audit;
    private readonly IAzureDevOpsReleaseSyncService _azureDevOpsSync;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly ILogger<ReleasesController> _logger;

    public ReleasesController(
        IReleaseAppService releaseAppService,
        IReleaseWorkflowService workflowService,
        IReadinessService readiness,
        IPostReleaseService postRelease,
        IPlanningService planning,
        IGovernanceService governance,
        IApplicationDbContext dbContext,
        IReleaseAuthorizationService authorization,
        ICurrentUserService currentUser,
        IClock clock,
        IFileStorageService fileStorage,
        IAuditService audit,
        IAzureDevOpsReleaseSyncService azureDevOpsSync,
        UserManager<AppIdentityUser> userManager,
        ILogger<ReleasesController> logger)
    {
        _releaseAppService = releaseAppService;
        _workflowService = workflowService;
        _readiness = readiness;
        _postRelease = postRelease;
        _planning = planning;
        _governance = governance;
        _dbContext = dbContext;
        _authorization = authorization;
        _currentUser = currentUser;
        _clock = clock;
        _fileStorage = fileStorage;
        _audit = audit;
        _azureDevOpsSync = azureDevOpsSync;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
    {
        var items = await _releaseAppService.GetMyReleasesAsync(cancellationToken);
        return View(items.Select(MapListItem).ToArray());
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanReviewRelease)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _releaseAppService.GetMyReleasesAsync(cancellationToken);
        return View("All", items.Select(MapListItem).ToArray());
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanCreateRelease)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await BuildWizardAsync(new ReleaseWizardViewModel(), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanCreateRelease)]
    public async Task<IActionResult> Create(
        ReleaseWizardViewModel model,
        string action,
        CancellationToken cancellationToken)
    {
        model = await BuildWizardAsync(model, cancellationToken);

        if (TryMoveStep(model, action))
        {
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = MapCreateRequest(model);
            var id = await _releaseAppService.CreateDraftAsync(request, cancellationToken);

            if (action == "submit")
            {
                await _workflowService.SubmitAsync(id, cancellationToken);
                TempData["Success"] = "Release submitted successfully.";
            }
            else
            {
                TempData["Success"] = "Draft saved.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or FluentValidation.ValidationException or DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var details = await _releaseAppService.GetByIdAsync(id, cancellationToken);
        var model = await BuildWizardAsync(new ReleaseWizardViewModel
        {
            ReleaseId = details.Id,
            Title = details.Title,
            Description = details.Description,
            ProductId = details.ProductId,
            EnvironmentId = details.EnvironmentId,
            PlannedReleaseDate = details.PlannedReleaseDate.Date,
            ReleaseType = details.ReleaseType,
            Priority = details.Priority,
            ReleaseVersion = details.ReleaseVersion,
            TestingSummary = details.TestingSummary,
            RiskLevel = details.RiskLevel,
            RiskDescription = details.RiskDescription,
            DeploymentPlan = details.DeploymentPlan,
            RollbackPlan = details.RollbackPlan,
            MonitoringPlan = details.MonitoringPlan,
            PostReleaseValidationPlan = details.PostReleaseValidationPlan,
            DowntimeRequired = details.DowntimeRequired,
            ExpectedDowntimeMinutes = details.ExpectedDowntimeMinutes,
            Services = details.Services.Select(MapServiceRow).ToList(),
            PlannedWindowStart = ToLocal(details.PlannedWindowStart),
            PlannedWindowEnd = ToLocal(details.PlannedWindowEnd),
            PlannedMaintenance = details.PlannedMaintenance,
            MaintenanceApprovalReference = details.MaintenanceApprovalReference,
            OperationalImpact = details.OperationalImpact,
            KeyDependencies = details.KeyDependencies,
            ImpactDescription = details.ImpactDescription,
            TechnicalOwnerUserId = details.TechnicalOwnerUserId,
            ForecastId = details.ForecastId,
            Criteria = ExpandFlags(details.ClassificationCriteria),
            Triggers = ExpandFlags(details.SecurityTriggers),
            ExecutionMode = details.ExecutionMode,
            ExpeditedJustification = details.ExpeditedJustification,
            DirectorApprovalReference = details.DirectorApprovalReference,
            RecoveryApproach = details.RecoveryApproach,
            RecoveryDecisionPoints = details.RecoveryDecisionPoints,
            RecoveryResponsibleParties = details.RecoveryResponsibleParties,
            References = details.References
                .Select(item => new ReleaseReferenceRowViewModel
                {
                    ReferenceType = item.ReferenceType,
                    ExternalId = item.ExternalId,
                    Url = item.Url,
                    Title = item.Title
                })
                .ToList()
        }, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        ReleaseWizardViewModel model,
        string action,
        CancellationToken cancellationToken)
    {
        if (model.ReleaseId is null)
        {
            return BadRequest();
        }

        model = await BuildWizardAsync(model, cancellationToken);

        if (TryMoveStep(model, action))
        {
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var update = MapUpdateRequest(model);
            await _releaseAppService.UpdateDraftAsync(update, cancellationToken);

            if (action == "submit")
            {
                await _workflowService.SubmitAsync(model.ReleaseId.GetValueOrDefault(), cancellationToken);
                TempData["Success"] = "Release submitted successfully.";
            }
            else
            {
                TempData["Success"] = "Draft updated.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ReleaseId });
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or FluentValidation.ValidationException or ConflictException or DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, cancellationToken);
        return View(page);
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildDetailsPageAsync(id, cancellationToken);
        return View(page);
    }

    [HttpGet]
    public async Task<IActionResult> Compliance(Guid id, CancellationToken cancellationToken)
    {
        var pack = await _governance.GetCompliancePackAsync(id, cancellationToken);
        return View(pack);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transition(TransitionFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _workflowService.TransitionAsync(
                new TransitionReleaseRequest
                {
                    ReleaseId = model.ReleaseId,
                    TargetStatus = model.TargetStatus,
                    Comment = model.Comment,
                    StabilizationEndUtc = model.StabilizationEnd.HasValue ? ToUtc(model.StabilizationEnd.Value) : null,
                    StabilizationNotes = model.StabilizationNotes
                },
                cancellationToken);

            TempData["Success"] = "Status updated.";
        }
        catch (Exception exception) when (IsHandled(exception))
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.ReleaseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _workflowService.SubmitAsync(id, cancellationToken);
            TempData["Success"] = "Release submitted.";
        }
        catch (Exception exception) when (IsHandled(exception))
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------ procedure v4.0 actions

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddReference(AddReleaseReferenceRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Reference linked.", () => _readiness.AddReferenceAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RemoveReference(Guid releaseId, Guid referenceId, CancellationToken cancellationToken) =>
        RunAsync(releaseId, "Reference removed.", () => _readiness.RemoveReferenceAsync(releaseId, referenceId, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SetReadiness(SetReadinessControlRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Readiness control updated.", () => _readiness.SetControlAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> LogCommunication(LogCommunicationRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Communication logged.", () => _readiness.LogCommunicationAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reschedule(
        Guid releaseId,
        DateTime plannedWindowStart,
        DateTime plannedWindowEnd,
        string reason,
        bool notifyStakeholders,
        string? audience,
        CancellationToken cancellationToken) =>
        RunAsync(releaseId, "Release rescheduled.", () => _readiness.RescheduleAsync(
            new RescheduleReleaseRequest
            {
                ReleaseId = releaseId,
                PlannedWindowStartUtc = ToUtc(plannedWindowStart),
                PlannedWindowEndUtc = ToUtc(plannedWindowEnd),
                Reason = reason,
                NotifyStakeholders = notifyStakeholders,
                Audience = audience
            },
            cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AssignOwners(Guid releaseId, Guid? technicalOwnerUserId, Guid? releaseManagerUserId, CancellationToken cancellationToken) =>
        RunAsync(releaseId, "Owners updated.", () => _readiness.AssignOwnersAsync(releaseId, technicalOwnerUserId, releaseManagerUserId, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> TechnicalValidation(RecordTechnicalValidationRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Technical validation recorded.", () => _postRelease.RecordTechnicalValidationAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> BusinessValidation(RecordBusinessValidationRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Business validation recorded.", () => _postRelease.RecordBusinessValidationAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RecordOutcome(RecordOutcomeRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Outcome recorded.", () => _postRelease.RecordOutcomeAsync(model, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenReview(Guid releaseId, CancellationToken cancellationToken)
    {
        try
        {
            var reviewId = await _postRelease.OpenManualReviewAsync(releaseId, cancellationToken);
            return RedirectToAction("Details", "Reviews", new { id = reviewId });
        }
        catch (Exception exception) when (IsHandled(exception))
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id = releaseId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddFreezeException(AddFreezeExceptionRequest model, CancellationToken cancellationToken) =>
        RunAsync(model.ReleaseId, "Freeze exception recorded.", () => _planning.AddFreezeExceptionAsync(model, cancellationToken));

    // ------------------------------------------------------------ comments / attachments

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(CommentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Comment is required.";
            return RedirectToAction(nameof(Details), new { id = model.ReleaseId });
        }

        await _authorization.EnsureCanViewAsync(model.ReleaseId, cancellationToken);

        var comment = new ReleaseComment(
            Guid.NewGuid(),
            model.ReleaseId,
            _currentUser.UserId,
            model.Comment,
            CommentType.General,
            isInternal: false,
            parentCommentId: model.ParentCommentId,
            _clock.UtcNow);

        _dbContext.ReleaseComments.Add(comment);
        await _audit.WriteAsync(
            "Release.AddComment",
            nameof(Release),
            model.ReleaseId.ToString(),
            null,
            new { model.Comment },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var release = await _dbContext.Releases
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == model.ReleaseId, cancellationToken);

        if (release is not null && release.AzureDevOpsWorkItemId.HasValue)
        {
            try
            {
                var adoText = string.IsNullOrWhiteSpace(_currentUser.UserName)
                    ? model.Comment
                    : $"{_currentUser.UserName}:\n{model.Comment}";

                await _azureDevOpsSync.AddCommentIfNeededAsync(release, adoText, cancellationToken);
                TempData["Success"] = "Comment added and synced to Azure DevOps Discussion.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Comment saved locally but Azure DevOps Discussion sync failed for release {ReleaseId}",
                    model.ReleaseId);
                TempData["Success"] = "Comment added.";
                TempData["Error"] = $"Azure DevOps Discussion sync failed: {exception.Message}";
            }
        }
        else
        {
            TempData["Success"] = "Comment added.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ReleaseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(
        Guid releaseId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Choose a file to upload.";
            return RedirectToAction(nameof(Details), new { id = releaseId });
        }

        await using var stream = file.OpenReadStream();
        var stored = await _fileStorage.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        var attachment = new ReleaseAttachment(
            Guid.NewGuid(),
            releaseId,
            stored.StoredFileName,
            file.FileName,
            stored.ContentType,
            stored.FileSize,
            stored.StoragePath,
            _currentUser.UserId,
            _clock.UtcNow,
            AttachmentType.General);

        _dbContext.ReleaseAttachments.Add(attachment);
        await _audit.WriteAsync(
            "Release.UploadAttachment",
            nameof(Release),
            releaseId.ToString(),
            null,
            new { file.FileName, stored.FileSize },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Attachment uploaded. Remember: the authoritative evidence should stay in the source system and be linked as a reference (§1.3).";
        return RedirectToAction(nameof(Details), new { id = releaseId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await _dbContext.ReleaseAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (attachment is null)
        {
            return NotFound();
        }

        await _authorization.EnsureCanViewAsync(attachment.ReleaseId, cancellationToken);
        var stream = await _fileStorage.DownloadAsync(attachment.StoragePath, cancellationToken);
        return File(stream, attachment.ContentType, attachment.OriginalFileName);
    }

    // ------------------------------------------------------------ helpers

    private async Task<IActionResult> RunAsync(Guid releaseId, string successMessage, Func<Task> action)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (Exception exception) when (IsHandled(exception))
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = releaseId });
    }

    private static bool IsHandled(Exception exception) =>
        exception is BusinessRuleException
            or ForbiddenException
            or ConflictException
            or NotFoundException
            or Domain.Exceptions.InvalidReleaseTransitionException
            or FluentValidation.ValidationException
            or DbUpdateConcurrencyException;

    private bool TryMoveStep(ReleaseWizardViewModel model, string action)
    {
        if (action == "next")
        {
            model.Step = Math.Min(WizardSteps, model.Step + 1);
            ModelState.Remove(nameof(model.Step));
            return true;
        }

        if (action == "previous")
        {
            model.Step = Math.Max(1, model.Step - 1);
            ModelState.Remove(nameof(model.Step));
            return true;
        }

        return false;
    }

    private async Task<ReleaseDetailsPageViewModel> BuildDetailsPageAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var details = await _releaseAppService.GetByIdAsync(id, cancellationToken);

        var productName = await _dbContext.Products.AsNoTracking()
            .Where(item => item.Id == details.ProductId)
            .Select(item => item.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var environmentName = await _dbContext.Environments.AsNoTracking()
            .Where(item => item.Id == details.EnvironmentId)
            .Select(item => item.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var history = await _dbContext.ReleaseStatusHistories.AsNoTracking()
            .Where(item => item.ReleaseId == id)
            .OrderByDescending(item => item.ChangedDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var comments = await _dbContext.ReleaseComments.AsNoTracking()
            .Where(item => item.ReleaseId == id)
            .OrderBy(item => item.CreatedDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        var userIds = comments.Select(item => item.UserId)
            .Concat(new[] { details.TechnicalOwnerUserId ?? Guid.Empty, details.ReleaseManagerUserId ?? Guid.Empty })
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToArray();

        var userNames = await _dbContext.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        var attachments = await _dbContext.ReleaseAttachments.AsNoTracking()
            .Where(item => item.ReleaseId == id)
            .OrderByDescending(item => item.UploadedDate)
            .ToListAsync(cancellationToken);

        var approvals = await _dbContext.ReleaseApprovals.AsNoTracking()
            .Where(item => item.ReleaseId == id)
            .OrderBy(item => item.Sequence)
            .ToListAsync(cancellationToken);

        var serviceIds = details.Services.Select(item => item.ServiceId).Distinct().ToArray();
        var serviceNames = await _dbContext.Services.AsNoTracking()
            .Where(item => serviceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        var forecastTitle = details.ForecastId.HasValue
            ? await _dbContext.ReleaseForecasts.AsNoTracking()
                .Where(item => item.Id == details.ForecastId.Value)
                .Select(item => $"Q{item.Quarter} {item.Year} — {item.Title}")
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var users = await _dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new LookupItemViewModel { Id = user.Id, Name = user.FullName })
            .ToListAsync(cancellationToken);

        var serviceRows = details.Services
            .Select(item =>
            {
                var row = MapServiceRow(item);
                row.ServiceName = serviceNames.GetValueOrDefault(item.ServiceId);
                return row;
            })
            .ToArray();

        var isRm = _currentUser.IsInRole(RoleNames.ReleaseManager) || _currentUser.IsInRole(RoleNames.Administrator);
        var isTo = _currentUser.IsInRole(RoleNames.TechnicalOwner) ||
                   details.TechnicalOwnerUserId == _currentUser.UserId ||
                   _currentUser.IsInRole(RoleNames.Administrator);
        var isPo = details.CreatedByUserId == _currentUser.UserId ||
                   _currentUser.IsInRole(RoleNames.ProductOwner) ||
                   _currentUser.IsInRole(RoleNames.Administrator);

        return new ReleaseDetailsPageViewModel
        {
            Id = details.Id,
            ReleaseNumber = details.ReleaseNumber,
            Title = details.Title,
            Description = details.Description,
            Priority = details.Priority,
            ReleaseVersion = details.ReleaseVersion,
            PlannedReleaseDate = details.PlannedReleaseDate,
            ProductName = productName,
            EnvironmentName = environmentName,
            TestingSummary = details.TestingSummary,
            RiskLevel = details.RiskLevel,
            RiskDescription = details.RiskDescription,
            DeploymentPlan = details.DeploymentPlan,
            RollbackPlan = details.RollbackPlan,
            MonitoringPlan = details.MonitoringPlan,
            PostReleaseValidationPlan = details.PostReleaseValidationPlan,
            DowntimeRequired = details.DowntimeRequired,
            ExpectedDowntimeMinutes = details.ExpectedDowntimeMinutes,
            AzureDevOpsWorkItemId = details.AzureDevOpsWorkItemId,
            AzureDevOpsWorkItemUrl = details.AzureDevOpsWorkItemUrl,
            Status = new ReleaseStatusSummaryViewModel
            {
                CurrentStatus = details.Status.CurrentStatus,
                CurrentStatusDisplay = details.Status.CurrentStatusDisplay,
                CurrentResponsibleDisplay = details.Status.CurrentResponsibleDisplay,
                StatusChangedAtUtc = details.Status.StatusChangedAtUtc,
                NextStageDisplay = details.Status.NextStageDisplay,
                ActionRequiredFromCurrentUser = details.Status.ActionRequiredFromCurrentUser,
                ReturnOrRejectReason = details.Status.ReturnOrRejectReason,
                ReturnedByDisplay = details.Status.ReturnedByDisplay,
                ReturnedDateUtc = details.Status.ReturnedDateUtc,
                PlannedReleaseDateUtc = details.Status.PlannedReleaseDateUtc
            },
            Services = serviceRows,
            AllowedTransitions = details.AllowedTransitions,
            History = history.Select(item => new StatusHistoryItemViewModel
            {
                FromStatus = ReleaseStatusDisplay.Format(item.FromStatus),
                ToStatus = ReleaseStatusDisplay.Format(item.ToStatus),
                Comment = item.Comment,
                ChangedDate = item.ChangedDate,
                ResponsibleRole = item.ResponsibleRole
            }).ToArray(),
            Comments = comments.Select(item => new CommentItemViewModel
            {
                Id = item.Id,
                ParentCommentId = item.ParentCommentId,
                Author = userNames.GetValueOrDefault(item.UserId, "Unknown"),
                Comment = item.Comment,
                CreatedDate = item.CreatedDate,
                IsInternal = item.IsInternal
            }).ToArray(),
            Attachments = attachments.Select(item => new AttachmentItemViewModel
            {
                Id = item.Id,
                OriginalFileName = item.OriginalFileName,
                ContentType = item.ContentType,
                FileSize = item.FileSize,
                UploadedDate = item.UploadedDate
            }).ToArray(),
            Approvals = approvals.Select(item => new ApprovalItemViewModel
            {
                ApprovalType = item.ApprovalType.ToString(),
                Status = item.Status.ToString(),
                Comment = item.Comment,
                RequestedDate = item.RequestedDate,
                DecisionDate = item.DecisionDate
            }).ToArray(),
            Transition = new TransitionFormViewModel { ReleaseId = id },
            NewComment = new CommentFormViewModel { ReleaseId = id },
            Procedure = details,
            TechnicalOwnerName = details.TechnicalOwnerUserId is { } toId ? userNames.GetValueOrDefault(toId) : null,
            ReleaseManagerName = details.ReleaseManagerUserId is { } rmId ? userNames.GetValueOrDefault(rmId) : null,
            ForecastTitle = forecastTitle,
            Users = users,
            ActiveFreezes = details.FreezeConflicts.Select(item => new FreezeConflictRowViewModel
            {
                FreezePeriodId = item.FreezePeriodId,
                Name = item.Name,
                FreezeType = item.FreezeType.ToString(),
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Authority = item.Authority,
                HasException = item.HasException
            }).ToArray(),
            IsReleaseManager = isRm,
            IsTechnicalOwner = isTo,
            IsProductOwner = isPo,
            CanEditRecord = isRm || isTo || details.CreatedByUserId == _currentUser.UserId
        };
    }

    private async Task<ReleaseWizardViewModel> BuildWizardAsync(
        ReleaseWizardViewModel model,
        CancellationToken cancellationToken)
    {
        model.Products = await _dbContext.Products.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

        model.Environments = await _dbContext.Environments.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

        if (model.ProductId != Guid.Empty)
        {
            model.AvailableServices = await _dbContext.Services.AsNoTracking()
                .Where(item => item.ProductId == model.ProductId && item.IsActive)
                .OrderBy(item => item.Name)
                .Select(item => new LookupItemViewModel { Id = item.Id, Name = item.Name })
                .ToListAsync(cancellationToken);

            var forecasts = await _planning.GetOpenForecastsForProductAsync(model.ProductId, cancellationToken);
            model.Forecasts = forecasts
                .Select(item => new LookupItemViewModel
                {
                    Id = item.Id,
                    Name = $"Q{item.Quarter} {item.Year} — {item.Title} ({item.Category})"
                })
                .ToArray();
        }

        var technicalOwners = await _userManager.GetUsersInRoleAsync(RoleNames.TechnicalOwner);
        var releaseManagers = await _userManager.GetUsersInRoleAsync(RoleNames.ReleaseManager);
        model.TechnicalOwners = technicalOwners
            .Concat(releaseManagers)
            .Where(user => user.IsActive)
            .GroupBy(user => user.Id)
            .Select(group => group.First())
            .OrderBy(user => user.FullName)
            .Select(user => new LookupItemViewModel { Id = user.Id, Name = user.FullName })
            .ToArray();

        if (model.PlannedWindowEnd > model.PlannedWindowStart)
        {
            var conflicts = await _planning.GetFreezeConflictsAsync(
                model.ReleaseId,
                ToUtc(model.PlannedWindowStart),
                ToUtc(model.PlannedWindowEnd),
                cancellationToken);

            model.FreezeWarnings = conflicts
                .Where(item => !item.HasException)
                .Select(item => $"{item.Name} ({item.FreezeType}) {item.StartDate.ToLocalTime():g} – {item.EndDate.ToLocalTime():g}, authority: {item.Authority}")
                .ToArray();
        }

        if (model.Services.Count == 0)
        {
            model.Services.Add(new ReleaseServiceRowViewModel());
        }

        if (model.References.Count == 0)
        {
            model.References.Add(new ReleaseReferenceRowViewModel());
        }

        return model;
    }

    private static CreateReleaseDraftRequest MapCreateRequest(ReleaseWizardViewModel model) =>
        new()
        {
            Title = model.Title,
            Description = model.Description,
            ProductId = model.ProductId,
            EnvironmentId = model.EnvironmentId,
            PlannedReleaseDateUtc = ToUtc(model.PlannedWindowStart),
            ReleaseType = model.ReleaseType,
            Priority = model.Priority,
            ReleaseVersion = model.ReleaseVersion,
            BusinessReason = string.Empty,
            ImpactDescription = model.ImpactDescription,
            TestingSummary = model.TestingSummary,
            RiskLevel = model.RiskLevel,
            RiskDescription = model.RiskDescription,
            DeploymentPlan = model.DeploymentPlan,
            RollbackPlan = model.RollbackPlan,
            MonitoringPlan = model.MonitoringPlan,
            PostReleaseValidationPlan = model.PostReleaseValidationPlan,
            DowntimeRequired = model.DowntimeRequired,
            ExpectedDowntimeMinutes = model.DowntimeRequired ? model.ExpectedDowntimeMinutes : null,
            Services = model.Services
                .Where(item => item.ServiceId != Guid.Empty)
                .Select(MapServiceDto)
                .ToList(),
            ClassificationCriteria = model.CriteriaFlags,
            SecurityTriggers = model.TriggerFlags,
            ExecutionMode = model.ExecutionMode,
            ExpeditedJustification = model.ExpeditedJustification,
            DirectorApprovalReference = model.DirectorApprovalReference,
            PlannedWindowStartUtc = ToUtc(model.PlannedWindowStart),
            PlannedWindowEndUtc = ToUtc(model.PlannedWindowEnd),
            PlannedMaintenance = model.PlannedMaintenance,
            MaintenanceApprovalReference = model.MaintenanceApprovalReference,
            OperationalImpact = model.OperationalImpact,
            KeyDependencies = model.KeyDependencies,
            RecoveryApproach = model.RecoveryApproach,
            RecoveryDecisionPoints = model.RecoveryDecisionPoints,
            RecoveryResponsibleParties = model.RecoveryResponsibleParties,
            TechnicalOwnerUserId = model.TechnicalOwnerUserId,
            ForecastId = model.ForecastId,
            References = model.References
                .Where(item => !string.IsNullOrWhiteSpace(item.ExternalId))
                .Select(item => new ReleaseReferenceInputDto
                {
                    ReferenceType = item.ReferenceType,
                    ExternalId = item.ExternalId!.Trim(),
                    Url = item.Url,
                    Title = item.Title
                })
                .ToList()
        };

    private static UpdateReleaseDraftRequest MapUpdateRequest(ReleaseWizardViewModel model)
    {
        var create = MapCreateRequest(model);
        return new UpdateReleaseDraftRequest
        {
            ReleaseId = model.ReleaseId!.Value,
            Title = create.Title,
            Description = create.Description,
            ProductId = create.ProductId,
            EnvironmentId = create.EnvironmentId,
            PlannedReleaseDateUtc = create.PlannedReleaseDateUtc,
            ReleaseType = create.ReleaseType,
            Priority = create.Priority,
            ReleaseVersion = create.ReleaseVersion,
            BusinessReason = create.BusinessReason,
            ImpactDescription = create.ImpactDescription,
            TestingSummary = create.TestingSummary,
            RiskLevel = create.RiskLevel,
            RiskDescription = create.RiskDescription,
            DeploymentPlan = create.DeploymentPlan,
            RollbackPlan = create.RollbackPlan,
            MonitoringPlan = create.MonitoringPlan,
            PostReleaseValidationPlan = create.PostReleaseValidationPlan,
            DowntimeRequired = create.DowntimeRequired,
            ExpectedDowntimeMinutes = create.ExpectedDowntimeMinutes,
            Services = create.Services,
            ClassificationCriteria = create.ClassificationCriteria,
            SecurityTriggers = create.SecurityTriggers,
            ExecutionMode = create.ExecutionMode,
            ExpeditedJustification = create.ExpeditedJustification,
            DirectorApprovalReference = create.DirectorApprovalReference,
            PlannedWindowStartUtc = create.PlannedWindowStartUtc,
            PlannedWindowEndUtc = create.PlannedWindowEndUtc,
            PlannedMaintenance = create.PlannedMaintenance,
            MaintenanceApprovalReference = create.MaintenanceApprovalReference,
            OperationalImpact = create.OperationalImpact,
            KeyDependencies = create.KeyDependencies,
            RecoveryApproach = create.RecoveryApproach,
            RecoveryDecisionPoints = create.RecoveryDecisionPoints,
            RecoveryResponsibleParties = create.RecoveryResponsibleParties,
            TechnicalOwnerUserId = create.TechnicalOwnerUserId,
            ForecastId = create.ForecastId,
            References = create.References
        };
    }

    private static ReleaseServiceInputDto MapServiceDto(ReleaseServiceRowViewModel item) =>
        new()
        {
            ServiceId = item.ServiceId,
            BranchName = item.BranchName,
            CommitId = item.CommitId,
            Version = item.Version,
            BuildNumber = item.BuildNumber,
            ArtifactUrl = null,
            RepositoryUrl = item.RepositoryUrl,
            DatabaseChanges = item.DatabaseChanges,
            ConfigurationChanges = item.ConfigurationChanges,
            Notes = item.Notes
        };

    private static ReleaseServiceRowViewModel MapServiceRow(ReleaseServiceInputDto item) =>
        new()
        {
            ServiceId = item.ServiceId,
            BranchName = item.BranchName,
            CommitId = item.CommitId,
            Version = item.Version,
            BuildNumber = item.BuildNumber,
            RepositoryUrl = item.RepositoryUrl,
            DatabaseChanges = item.DatabaseChanges,
            ConfigurationChanges = item.ConfigurationChanges,
            Notes = item.Notes
        };

    private static ReleaseListItemViewModel MapListItem(ReleaseListItemDto item) =>
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
            ActionRequiredFromCurrentUser = item.ActionRequiredFromCurrentUser,
            Category = item.Category,
            ExecutionMode = item.ExecutionMode
        };

    private static List<T> ExpandFlags<T>(T flags)
        where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .Where(value => !EqualityComparer<T>.Default.Equals(value, default) && flags.HasFlag(value))
            .ToList();
    }

    /// <summary>datetime-local inputs are entered in the server's local time zone.</summary>
    private static DateTime ToUtc(DateTime local) =>
        DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

    private static DateTime ToLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
