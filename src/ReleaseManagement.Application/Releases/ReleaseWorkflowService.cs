using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Application.Planning;
using ReleaseManagement.Application.PostRelease;
using ReleaseManagement.Application.Readiness;
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
    private readonly IReadinessService _readiness;
    private readonly IPostReleaseService _postRelease;
    private readonly IPlanningService _planning;

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
        IValidator<TransitionReleaseRequest> transitionValidator,
        IReadinessService readiness,
        IPostReleaseService postRelease,
        IPlanningService planning)
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
        _readiness = readiness;
        _postRelease = postRelease;
        _planning = planning;
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
            .Include(item => item.Services)
            .Include(item => item.Approvals)
            .Include(item => item.References)
            .Include(item => item.ReadinessControls)
            .Include(item => item.Communications)
            .Include(item => item.DeploymentRecords)
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

        await EnsureGateAsync(release, request, cancellationToken);

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

        await ApplyPostTransitionEffectsAsync(release, previousStatus, request, cancellationToken);
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

            foreach (var reference in tracked.References.ToArray())
            {
                _dbContext.Detach(reference);
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
            .Include(item => item.References)
            .Include(item => item.ReadinessControls)
            .Include(item => item.Communications)
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

        await EnsureNoFreezeConflictAsync(release, cancellationToken);

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
                "Release record submitted",
                $"{release.ReleaseNumber} ({release.Category}, {release.ExecutionMode}) has been submitted. Check the minimum record and start readiness.",
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

    // ------------------------------------------------------------------ gates (§8.1 hard block)

    private async Task EnsureGateAsync(
        Release release,
        TransitionReleaseRequest request,
        CancellationToken cancellationToken)
    {
        switch (request.TargetStatus)
        {
            case ReleaseStatus.ReadyForRelease:
            {
                var environment = await _dbContext.Environments
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == release.EnvironmentId, cancellationToken);

                var errors = ReleaseReadinessRules.GetReadyForReleaseErrors(release, environment.IsProduction);
                if (errors.Count > 0)
                {
                    throw new BusinessRuleException(errors);
                }

                await EnsureNoFreezeConflictAsync(release, cancellationToken);
                break;
            }

            case ReleaseStatus.DeploymentInProgress:
            {
                if (release.ReadinessControls.Count > 0 &&
                    release.ReadinessControls.Any(item => !item.IsClosed))
                {
                    throw new BusinessRuleException(
                        "Deployment cannot start while readiness controls are open (§8.1).");
                }

                await EnsureNoFreezeConflictAsync(release, cancellationToken);
                break;
            }

            case ReleaseStatus.Stabilization:
            {
                var validation = await _dbContext.PostReleaseValidations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

                var errors = ReleaseReadinessRules.GetStabilizationErrors(release, validation);
                if (errors.Count > 0)
                {
                    throw new BusinessRuleException(errors);
                }

                break;
            }

            case ReleaseStatus.Closed:
            {
                var validation = await _dbContext.PostReleaseValidations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

                var review = await _dbContext.PostImplementationReviews
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

                // Outcome defaults to Successful when validation passed and no recovery happened.
                if (release.Outcome is null && validation is { IsPassed: true })
                {
                    release.RecordOutcome(ReleaseOutcome.Successful, null, _clock.UtcNow);
                }

                var errors = ReleaseReadinessRules.GetClosureErrors(release, validation, review, _clock.UtcNow);
                if (errors.Count > 0)
                {
                    throw new BusinessRuleException(errors);
                }

                break;
            }
        }
    }

    private async Task EnsureNoFreezeConflictAsync(Release release, CancellationToken cancellationToken)
    {
        var conflicts = await _planning.GetFreezeConflictsAsync(
            release.Id,
            release.PlannedWindowStart,
            release.PlannedWindowEnd,
            cancellationToken);

        var blocking = conflicts.Where(item => !item.HasException).ToArray();
        if (blocking.Length == 0)
        {
            return;
        }

        var names = string.Join(", ", blocking.Select(item => $"{item.Name} ({item.FreezeType}, {item.StartDate:d}–{item.EndDate:d}, authority: {item.Authority})"));
        throw new BusinessRuleException(
            $"The planned window falls into an active release freeze: {names}. Reschedule the window or record an exception approved by the freeze authority (§6.3).");
    }

    // ------------------------------------------------------------------ side effects

    private async Task ApplyPostTransitionEffectsAsync(
        Release release,
        ReleaseStatus previousStatus,
        TransitionReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        switch (release.CurrentStatus)
        {
            case ReleaseStatus.ReadinessInProgress:
            {
                // RM takes coordination ownership; TO defaults to RM when nobody is assigned (same person today).
                release.AssignOwners(
                    release.TechnicalOwnerUserId ?? _currentUser.UserId,
                    release.ReleaseManagerUserId ?? _currentUser.UserId,
                    now);

                _readiness.EnsureControls(release, now);

                foreach (var control in release.ReadinessControls.Where(item => item.IsRequired && !item.IsClosed))
                {
                    var definition = ReleaseReadinessRules.GetDefinition(control.ControlType);
                    await _notifications.CreateForRoleAsync(
                        definition.OwnerRole,
                        release.Id,
                        $"Release {release.ReleaseNumber}: {definition.Title} evidence required",
                        $"Set the readiness status for '{definition.Title}' and link the evidence in the source system ({definition.ProcedureSection}).",
                        NotificationType.ActionRequired,
                        cancellationToken);
                }

                break;
            }

            case ReleaseStatus.Deployed:
            {
                var exists = await _dbContext.PostReleaseValidations
                    .AnyAsync(item => item.ReleaseId == release.Id, cancellationToken);

                if (!exists)
                {
                    _dbContext.PostReleaseValidations.Add(
                        new PostReleaseValidation(
                            Guid.NewGuid(),
                            release.Id,
                            ReleaseReadinessRules.RequiresBusinessValidation(release),
                            now));
                }

                await _notifications.CreateForRoleAsync(
                    RoleNames.TechnicalOwner,
                    release.Id,
                    $"Release {release.ReleaseNumber}: post-release validation required",
                    "Record deployment result, health, smoke / technical validation, monitoring and recovery need (§7.2).",
                    NotificationType.ActionRequired,
                    cancellationToken);

                if (ReleaseReadinessRules.RequiresBusinessValidation(release))
                {
                    await _notifications.CreateAsync(
                        release.CreatedByUserId,
                        release.Id,
                        $"Release {release.ReleaseNumber}: business validation required",
                        "Confirm the business-visible change works as expected (§7.2).",
                        NotificationType.ActionRequired,
                        cancellationToken);
                }

                if (release.IsExpedited)
                {
                    await _postRelease.EnsureReviewAsync(release, PirTriggers.Expedited, cancellationToken);
                }

                if (release.ForecastId is { } forecastId)
                {
                    var forecast = await _dbContext.ReleaseForecasts
                        .SingleOrDefaultAsync(item => item.Id == forecastId, cancellationToken);
                    forecast?.MarkDelivered(now);
                }

                break;
            }

            case ReleaseStatus.Stabilization:
            {
                var end = request.StabilizationEndUtc.HasValue
                    ? DateTime.SpecifyKind(request.StabilizationEndUtc.Value, DateTimeKind.Utc)
                    : now.Add(DefaultStabilizationPeriod(release.Category));

                release.SetStabilization(now, end, request.StabilizationNotes, now);
                break;
            }

            case ReleaseStatus.DeploymentFailed:
            {
                await _postRelease.EnsureReviewAsync(release, PirTriggers.FailedDeployment, cancellationToken);
                break;
            }

            case ReleaseStatus.RolledBack:
            {
                await _postRelease.EnsureReviewAsync(release, PirTriggers.RollbackOrRemediation, cancellationToken);
                break;
            }

            case ReleaseStatus.Cancelled:
            {
                if (release.ForecastId is { } forecastId)
                {
                    var forecast = await _dbContext.ReleaseForecasts
                        .SingleOrDefaultAsync(item => item.Id == forecastId, cancellationToken);
                    if (forecast is not null)
                    {
                        forecast.Update(
                            forecast.Title,
                            forecast.Category,
                            forecast.Team,
                            forecast.ExpectedDate,
                            forecast.Dependencies,
                            forecast.Notes,
                            ForecastStatus.Cancelled,
                            now);
                    }
                }

                break;
            }
        }

        _ = previousStatus;
    }

    /// <summary>§7.2 — no universal rule; defaults are only a starting point the RM can override.</summary>
    private static TimeSpan DefaultStabilizationPeriod(ReleaseCategory category) => category switch
    {
        ReleaseCategory.Major => TimeSpan.FromHours(72),
        ReleaseCategory.Normal => TimeSpan.FromHours(24),
        _ => TimeSpan.FromHours(4)
    };

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
            ReleaseStatus.BusinessChangesRequired or
            ReleaseStatus.QaChangesRequired or
            ReleaseStatus.RiskChangesRequired or
            ReleaseStatus.ChapterLeadChangesRequired
            ? createdByUserId
            : null;
    }

    private async Task EnsureApprovalRecordAsync(
        Release release,
        ReleaseStatus previousStatus,
        TransitionReleaseRequest request,
        CancellationToken cancellationToken)
    {
        // Legacy in-app structure approvals (kept for releases still in those statuses).
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
            ReleaseStatus.BusinessChangesRequired or
            ReleaseStatus.QaChangesRequired or
            ReleaseStatus.RiskChangesRequired or
            ReleaseStatus.ChapterLeadChangesRequired => ApprovalStatus.ChangesRequired,
            ReleaseStatus.Rejected => ApprovalStatus.Rejected,
            ReleaseStatus.ReleaseManagerReview when previousStatus is
                ReleaseStatus.QaReview or
                ReleaseStatus.InfoSecReview or
                ReleaseStatus.RiskReview or
                ReleaseStatus.ChapterLeadReview or
                ReleaseStatus.PentestReview or
                ReleaseStatus.BusinessApproval
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
