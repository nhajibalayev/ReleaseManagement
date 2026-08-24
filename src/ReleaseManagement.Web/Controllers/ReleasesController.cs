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
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class ReleasesController : Controller
{
    private readonly IReleaseAppService _releaseAppService;
    private readonly IReleaseWorkflowService _workflowService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditService _audit;

    public ReleasesController(
        IReleaseAppService releaseAppService,
        IReleaseWorkflowService workflowService,
        IApplicationDbContext dbContext,
        IReleaseAuthorizationService authorization,
        ICurrentUserService currentUser,
        IClock clock,
        IFileStorageService fileStorage,
        IAuditService audit)
    {
        _releaseAppService = releaseAppService;
        _workflowService = workflowService;
        _dbContext = dbContext;
        _authorization = authorization;
        _currentUser = currentUser;
        _clock = clock;
        _fileStorage = fileStorage;
        _audit = audit;
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

        if (action == "next")
        {
            model.Step = Math.Min(5, model.Step + 1);
            ModelState.Remove(nameof(model.Step));
            return View(model);
        }

        if (action == "previous")
        {
            model.Step = Math.Max(1, model.Step - 1);
            ModelState.Remove(nameof(model.Step));
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
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or FluentValidation.ValidationException)
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
            Services = details.Services.Select(MapServiceRow).ToList()
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

        if (action == "next")
        {
            model.Step = Math.Min(5, model.Step + 1);
            ModelState.Remove(nameof(model.Step));
            return View(model);
        }

        if (action == "previous")
        {
            model.Step = Math.Max(1, model.Step - 1);
            ModelState.Remove(nameof(model.Step));
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
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or FluentValidation.ValidationException or ConflictException)
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
                    Comment = model.Comment
                },
                cancellationToken);

            TempData["Success"] = "Status updated.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException or Domain.Exceptions.InvalidReleaseTransitionException or FluentValidation.ValidationException)
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
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException or FluentValidation.ValidationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

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
            parentCommentId: null,
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

        TempData["Success"] = "Comment added.";
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

        TempData["Success"] = "Attachment uploaded.";
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
            .OrderByDescending(item => item.CreatedDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var commentAuthors = await _dbContext.Users.AsNoTracking()
            .Where(user => comments.Select(item => item.UserId).Contains(user.Id))
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

        var serviceRows = details.Services
            .Select(item =>
            {
                var row = MapServiceRow(item);
                row.ServiceName = serviceNames.GetValueOrDefault(item.ServiceId);
                return row;
            })
            .ToArray();

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
                Author = commentAuthors.GetValueOrDefault(item.UserId, "Unknown"),
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
            NewComment = new CommentFormViewModel { ReleaseId = id }
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
        }

        if (model.Services.Count == 0)
        {
            model.Services.Add(new ReleaseServiceRowViewModel());
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
            PlannedReleaseDateUtc = DateTime.SpecifyKind(model.PlannedReleaseDate.Date, DateTimeKind.Utc),
            ReleaseType = model.ReleaseType,
            Priority = model.Priority,
            ReleaseVersion = model.ReleaseVersion,
            BusinessReason = string.Empty,
            ImpactDescription = string.Empty,
            TestingSummary = model.TestingSummary,
            RiskLevel = model.RiskLevel,
            RiskDescription = model.RiskDescription,
            DeploymentPlan = model.DeploymentPlan,
            RollbackPlan = model.RollbackPlan,
            MonitoringPlan = model.MonitoringPlan,
            PostReleaseValidationPlan = model.PostReleaseValidationPlan,
            DowntimeRequired = model.DowntimeRequired,
            ExpectedDowntimeMinutes = model.ExpectedDowntimeMinutes,
            Services = model.Services
                .Where(item => item.ServiceId != Guid.Empty)
                .Select(MapServiceDto)
                .ToList()
        };

    private static UpdateReleaseDraftRequest MapUpdateRequest(ReleaseWizardViewModel model)
    {
        var request = new UpdateReleaseDraftRequest { ReleaseId = model.ReleaseId!.Value };
        var create = MapCreateRequest(model);
        request.Title = create.Title;
        request.Description = create.Description;
        request.ProductId = create.ProductId;
        request.EnvironmentId = create.EnvironmentId;
        request.PlannedReleaseDateUtc = create.PlannedReleaseDateUtc;
        request.ReleaseType = create.ReleaseType;
        request.Priority = create.Priority;
        request.ReleaseVersion = create.ReleaseVersion;
        request.BusinessReason = create.BusinessReason;
        request.ImpactDescription = create.ImpactDescription;
        request.TestingSummary = create.TestingSummary;
        request.RiskLevel = create.RiskLevel;
        request.RiskDescription = create.RiskDescription;
        request.DeploymentPlan = create.DeploymentPlan;
        request.RollbackPlan = create.RollbackPlan;
        request.MonitoringPlan = create.MonitoringPlan;
        request.PostReleaseValidationPlan = create.PostReleaseValidationPlan;
        request.DowntimeRequired = create.DowntimeRequired;
        request.ExpectedDowntimeMinutes = create.ExpectedDowntimeMinutes;
        request.Services = create.Services;
        return request;
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
            ActionRequiredFromCurrentUser = item.ActionRequiredFromCurrentUser
        };
}
