using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.Releases;

public sealed class ReleaseWorkflowService : IReleaseWorkflowService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IAzureDevOpsTokenProvider _azureDevOpsTokenProvider;
    private readonly IBackgroundJobSettings _backgroundJobs;
    private readonly IAzureDevOpsReleaseSyncService _azureDevOpsSync;
    private readonly IValidator<TransitionReleaseRequest> _transitionValidator;

    public ReleaseWorkflowService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IReleaseAuthorizationService authorization,
        IClock clock,
        INotificationService notifications,
        IAuditService audit,
        IOutboxWriter outbox,
        IAzureDevOpsTokenProvider azureDevOpsTokenProvider,
        IBackgroundJobSettings backgroundJobs,
        IAzureDevOpsReleaseSyncService azureDevOpsSync,
        IValidator<TransitionReleaseRequest> transitionValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _authorization = authorization;
        _clock = clock;
        _notifications = notifications;
        _audit = audit;
        _outbox = outbox;
        _azureDevOpsTokenProvider = azureDevOpsTokenProvider;
        _backgroundJobs = backgroundJobs;
        _azureDevOpsSync = azureDevOpsSync;
        _transitionValidator = transitionValidator;
    }

    public bool CanTransition(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        IReadOnlyCollection<string> userRoles)
    {
        return ReleaseWorkflowRules.CanTransition(currentStatus, targetStatus, userRoles);
    }

    public Task TransitionAsync(
        Guid releaseId,
        ReleaseStatus targetStatus,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            new TransitionReleaseRequest
            {
                ReleaseId = releaseId,
                TargetStatus = targetStatus,
                Comment = comment
            },
            cancellationToken);
    }

    public async Task TransitionAsync(
        TransitionReleaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _transitionValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        var release = await _dbContext.Releases
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == request.ReleaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), request.ReleaseId);
        }

        if (!CanTransition(release.CurrentStatus, request.TargetStatus, _currentUser.Roles))
        {
            throw new ForbiddenException(
                $"Transition from {release.CurrentStatus} to {request.TargetStatus} is not allowed for the current user.");
        }

        await EnsureDeploymentApprovalsAsync(release, request.TargetStatus, cancellationToken);

        var previousStatus = release.CurrentStatus;
        var responsibleRole = ReleaseStatusDisplay.GetDefaultResponsibleRole(request.TargetStatus);
        var responsibleUserId = ResolveResponsibleUserId(
            request.TargetStatus,
            request.AssignedUserId,
            release.CreatedByUserId);

        release.TransitionTo(
            request.TargetStatus,
            _currentUser.Roles,
            _currentUser.UserId,
            _clock.UtcNow,
            request.Comment,
            responsibleUserId,
            responsibleRole);

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            release.AddComment(
                new ReleaseComment(
                    Guid.NewGuid(),
                    release.Id,
                    _currentUser.UserId,
                    request.Comment!,
                    CommentType.Review,
                    isInternal: false,
                    parentCommentId: null,
                    _clock.UtcNow));
        }

        await EnsureApprovalRecordAsync(release, previousStatus, request, cancellationToken);
        await NotifyStakeholdersAsync(release, previousStatus, request.Comment, cancellationToken);

        await _audit.WriteAsync(
            "Release.Transition",
            nameof(Release),
            release.Id.ToString(),
            new { Status = previousStatus },
            new
            {
                Status = release.CurrentStatus,
                release.CurrentResponsibleRole,
                release.CurrentResponsibleUserId,
                request.Comment
            },
            cancellationToken);

        if (_backgroundJobs.HangfireEnabled)
        {
            await _outbox.EnqueueAsync(
                OutboxMessageTypes.AzureDevOpsUpdateWorkItem,
                JsonSerializer.Serialize(new
                {
                    ReleaseId = release.Id,
                    Status = release.CurrentStatus,
                    AccessToken = await _azureDevOpsTokenProvider.GetAccessTokenAsync(cancellationToken)
                }),
                idempotencyKey: $"ado-update:{release.Id}:{release.CurrentStatus}:{release.UpdatedDate:O}",
                cancellationToken);
        }
        else
        {
            await _azureDevOpsSync.UpdateWorkItemIfNeededAsync(release, cancellationToken);
        }

        foreach (var history in release.StatusHistory)
        {
            var exists = await _dbContext.ReleaseStatusHistories
                .AsNoTracking()
                .AnyAsync(item => item.Id == history.Id, cancellationToken);
            if (!exists)
            {
                _dbContext.ForceAdded(history);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanEditAsync(releaseId, cancellationToken);

        // Create+Submit shares one DbContext; detach tracked graph so Include loads cleanly.
        var tracked = _dbContext.Releases.Local.FirstOrDefault(item => item.Id == releaseId);
        if (tracked is not null)
        {
            foreach (var service in tracked.Services.ToArray())
            {
                _dbContext.Detach(service);
            }

            foreach (var approval in tracked.Approvals.ToArray())
            {
                _dbContext.Detach(approval);
            }

            foreach (var history in tracked.StatusHistory.ToArray())
            {
                _dbContext.Detach(history);
            }

            _dbContext.Detach(tracked);
        }

        var release = await _dbContext.Releases
            .Include(item => item.Services)
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), releaseId);
        }

        if (release.CurrentStatus is not (ReleaseStatus.Draft or ReleaseStatus.ReturnedForRevision))
        {
            throw new ConflictException(
                $"Release '{release.ReleaseNumber}' cannot be submitted from status {release.CurrentStatus}.");
        }

        var environment = await _dbContext.Environments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == release.EnvironmentId, cancellationToken);

        if (environment is null || !environment.IsActive)
        {
            throw new BusinessRuleException("The selected environment is missing or inactive.");
        }

        var readinessErrors = ReleaseReadinessRules.GetSubmissionErrors(
            release,
            environment.IsProduction);

        if (readinessErrors.Count > 0)
        {
            throw new BusinessRuleException(readinessErrors);
        }

        if (!CanTransition(release.CurrentStatus, ReleaseStatus.Submitted, _currentUser.Roles))
        {
            throw new ForbiddenException(
                $"Transition from {release.CurrentStatus} to {ReleaseStatus.Submitted} is not allowed for the current user.");
        }

        var now = _clock.UtcNow;
        var submittedFrom = release.CurrentStatus;

        release.TransitionTo(
            ReleaseStatus.Submitted,
            _currentUser.Roles,
            _currentUser.UserId,
            now,
            null,
            ResolveResponsibleUserId(ReleaseStatus.Submitted, null, release.CreatedByUserId),
            ReleaseStatusDisplay.GetDefaultResponsibleRole(ReleaseStatus.Submitted));

        await _audit.WriteAsync(
            "Release.Transition",
            nameof(Release),
            release.Id.ToString(),
            new { Status = submittedFrom },
            new
            {
                Status = release.CurrentStatus,
                release.CurrentResponsibleRole,
                release.CurrentResponsibleUserId
            },
            cancellationToken);

        // Auto-route into Release Manager review in the same unit of work (one SaveChanges).
        if (CanTransition(
                ReleaseStatus.Submitted,
                ReleaseStatus.ReleaseManagerReview,
                [RoleNames.ReleaseManager, RoleNames.Administrator]))
        {
            var reviewFrom = release.CurrentStatus;
            var responsibleRole = ReleaseStatusDisplay.GetDefaultResponsibleRole(
                ReleaseStatus.ReleaseManagerReview);

            release.TransitionTo(
                ReleaseStatus.ReleaseManagerReview,
                [RoleNames.Administrator],
                _currentUser.UserId,
                now,
                "Automatically routed to Release Manager review after submit.",
                null,
                responsibleRole);

            await _notifications.CreateForRoleAsync(
                RoleNames.ReleaseManager,
                release.Id,
                "Release ready for review",
                $"{release.ReleaseNumber} has been submitted and awaits Release Manager review.",
                NotificationType.ActionRequired,
                cancellationToken);

            await _audit.WriteAsync(
                "Release.Transition",
                nameof(Release),
                release.Id.ToString(),
                new { Status = reviewFrom },
                new
                {
                    Status = release.CurrentStatus,
                    release.CurrentResponsibleRole,
                    release.CurrentResponsibleUserId
                },
                cancellationToken);
        }

        if (_backgroundJobs.HangfireEnabled)
        {
            await _outbox.EnqueueAsync(
                OutboxMessageTypes.AzureDevOpsCreateWorkItem,
                JsonSerializer.Serialize(new
                {
                    ReleaseId = release.Id,
                    AccessToken = await _azureDevOpsTokenProvider.GetAccessTokenAsync(cancellationToken)
                }),
                idempotencyKey: $"ado-create:{release.Id}",
                cancellationToken);
        }
        else
        {
            await _azureDevOpsSync.CreateWorkItemIfNeededAsync(release, cancellationToken);
        }

        // StatusHistory is added on an unloaded collection; force INSERT so EF does not
        // emit UPDATE ... WHERE Id=... against rows that do not exist yet.
        foreach (var history in release.StatusHistory)
        {
            _dbContext.ForceAdded(history);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid? ResolveResponsibleUserId(
        ReleaseStatus targetStatus,
        Guid? assignedUserId,
        Guid createdByUserId)
    {
        if (assignedUserId.HasValue && assignedUserId.Value != Guid.Empty)
        {
            return assignedUserId;
        }

        return targetStatus is
            ReleaseStatus.Draft or
            ReleaseStatus.ReturnedForRevision or
            ReleaseStatus.PentestChangesRequired or
            ReleaseStatus.InfoSecChangesRequired or
            ReleaseStatus.BusinessChangesRequired
            ? createdByUserId
            : null;
    }

    private async Task EnsureDeploymentApprovalsAsync(
        Release release,
        ReleaseStatus targetStatus,
        CancellationToken cancellationToken)
    {
        if (targetStatus != ReleaseStatus.DeploymentInProgress)
        {
            return;
        }

        var approvals = await _dbContext.ReleaseApprovals
            .AsNoTracking()
            .Where(item => item.ReleaseId == release.Id)
            .ToListAsync(cancellationToken);

        if (!ReleaseReadinessRules.HasRequiredApprovals(approvals))
        {
            throw new BusinessRuleException(
                "Deployment cannot start until all required approvals are completed.");
        }
    }

    private async Task EnsureApprovalRecordAsync(
        Release release,
        ReleaseStatus previousStatus,
        TransitionReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var approvalType = ReleaseStatusDisplay.MapStatusToApprovalType(previousStatus);
        if (approvalType is null)
        {
            return;
        }

        ApprovalStatus? decision = request.TargetStatus switch
        {
            ReleaseStatus.ReturnedForRevision or
            ReleaseStatus.PentestChangesRequired or
            ReleaseStatus.InfoSecChangesRequired or
            ReleaseStatus.BusinessChangesRequired => ApprovalStatus.ChangesRequired,
            ReleaseStatus.Rejected => ApprovalStatus.Rejected,
            ReleaseStatus.PentestReview when previousStatus == ReleaseStatus.ReleaseManagerReview
                => ApprovalStatus.Approved,
            ReleaseStatus.InfoSecReview when previousStatus == ReleaseStatus.PentestReview
                => ApprovalStatus.Approved,
            ReleaseStatus.BusinessApproval when previousStatus == ReleaseStatus.InfoSecReview
                => ApprovalStatus.Approved,
            ReleaseStatus.Approved when previousStatus == ReleaseStatus.BusinessApproval
                => ApprovalStatus.Approved,
            _ => null
        };

        if (decision is null)
        {
            return;
        }

        var pending = await _dbContext.ReleaseApprovals
            .Where(item =>
                item.ReleaseId == release.Id &&
                item.ApprovalType == approvalType &&
                item.Status == ApprovalStatus.Pending)
            .OrderByDescending(item => item.RequestedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is null)
        {
            pending = new ReleaseApproval(
                Guid.NewGuid(),
                release.Id,
                approvalType.Value,
                sequence: (int)approvalType.Value,
                assignedUserId: _currentUser.UserId,
                assignedRole: ReleaseStatusDisplay.GetDefaultResponsibleRole(previousStatus),
                isRequired: true,
                requestedDateUtc: _clock.UtcNow);
            release.AddApproval(pending);
            _dbContext.ReleaseApprovals.Add(pending);
        }

        pending.Decide(decision.Value, _currentUser.UserId, _clock.UtcNow, request.Comment);
    }

    private async Task NotifyStakeholdersAsync(
        Release release,
        ReleaseStatus previousStatus,
        string? comment,
        CancellationToken cancellationToken)
    {
        var title = $"Release {release.ReleaseNumber}: {ReleaseStatusDisplay.Format(release.CurrentStatus)}";
        var message =
            $"Status changed from {ReleaseStatusDisplay.Format(previousStatus)} to {ReleaseStatusDisplay.Format(release.CurrentStatus)}.";

        if (!string.IsNullOrWhiteSpace(comment))
        {
            message = $"{message} Comment: {comment}";
        }

        await _notifications.CreateAsync(
            release.CreatedByUserId,
            release.Id,
            title,
            message,
            NotificationType.Information,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(release.CurrentResponsibleRole))
        {
            await _notifications.CreateForRoleAsync(
                release.CurrentResponsibleRole,
                release.Id,
                title,
                message,
                NotificationType.ActionRequired,
                cancellationToken);
        }
        else if (release.CurrentResponsibleUserId.HasValue)
        {
            await _notifications.CreateAsync(
                release.CurrentResponsibleUserId.Value,
                release.Id,
                title,
                message,
                NotificationType.ActionRequired,
                cancellationToken);
        }
    }
}
