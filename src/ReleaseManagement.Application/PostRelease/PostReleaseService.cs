using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.PostRelease;

/// <summary>Procedure v4.0 §7.2 / §7.3 / §8.2 — validation, stabilization, outcome and PIR.</summary>
public interface IPostReleaseService
{
    Task RecordTechnicalValidationAsync(RecordTechnicalValidationRequest request, CancellationToken cancellationToken = default);

    Task RecordBusinessValidationAsync(RecordBusinessValidationRequest request, CancellationToken cancellationToken = default);

    Task RecordOutcomeAsync(RecordOutcomeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates the PIR when triggers exist (idempotent, adds new triggers). Does not save.</summary>
    Task<PostImplementationReview?> EnsureReviewAsync(Release release, PirTriggers extraTriggers, CancellationToken cancellationToken = default);

    Task<Guid> OpenManualReviewAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task UpdateReviewAsync(UpdatePirRequest request, CancellationToken cancellationToken = default);

    Task AddActionAsync(AddPirActionRequest request, CancellationToken cancellationToken = default);

    Task CompleteActionAsync(Guid reviewId, Guid actionId, CancellationToken cancellationToken = default);

    Task CompleteReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task<PostImplementationReviewDto?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PostImplementationReviewDto>> ListReviewsAsync(CancellationToken cancellationToken = default);
}

public sealed class PostReleaseService : IPostReleaseService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public PostReleaseService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IReleaseAuthorizationService authorization,
        IClock clock,
        IAuditService audit,
        INotificationService notifications)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _authorization = authorization;
        _clock = clock;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task RecordTechnicalValidationAsync(
        RecordTechnicalValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);
        EnsureRole("Technical / Service Owner", RoleNames.TechnicalOwner, RoleNames.DevOps, RoleNames.ITOperations);

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);
        EnsurePostDeployment(release);

        var now = _clock.UtcNow;
        var validation = await GetOrCreateValidationAsync(release, now, cancellationToken);

        try
        {
            validation.RecordTechnical(
                request.Result,
                request.HealthCheckPassed,
                request.SmokeTestPassed,
                request.MonitoringClean,
                request.RecoveryNeeded,
                request.Notes,
                request.EvidenceReference,
                _currentUser.UserId,
                now);
        }
        catch (ArgumentException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        if (request.Result == ValidationResult.Failed)
        {
            await EnsureReviewAsync(release, PirTriggers.FailedValidation, cancellationToken);
            await _notifications.CreateForRoleAsync(
                RoleNames.ReleaseManager,
                release.Id,
                $"Release {release.ReleaseNumber}: post-release validation failed",
                request.Notes ?? "Technical validation failed — decide on rollback / remediation.",
                NotificationType.Warning,
                cancellationToken);
        }

        await _audit.WriteAsync(
            "Release.TechnicalValidation",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { request.Result, request.HealthCheckPassed, request.SmokeTestPassed, request.MonitoringClean, request.RecoveryNeeded },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordBusinessValidationAsync(
        RecordBusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);
        EnsureRole("Product Owner", RoleNames.ProductOwner);

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);
        EnsurePostDeployment(release);

        var now = _clock.UtcNow;
        var validation = await GetOrCreateValidationAsync(release, now, cancellationToken);

        try
        {
            validation.RecordBusiness(request.Result, request.Notes, _currentUser.UserId, now);
        }
        catch (ArgumentException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        if (request.Result == ValidationResult.Failed)
        {
            await EnsureReviewAsync(release, PirTriggers.FailedValidation, cancellationToken);
        }

        await _audit.WriteAsync(
            "Release.BusinessValidation",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { request.Result, request.Notes },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordOutcomeAsync(
        RecordOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);
        EnsureRole("Release Manager", RoleNames.ReleaseManager);

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);

        if (release.CurrentStatus is not (
            ReleaseStatus.Deployed or
            ReleaseStatus.Stabilization or
            ReleaseStatus.DeploymentFailed or
            ReleaseStatus.RolledBack))
        {
            throw new ConflictException("The outcome can only be recorded after deployment.");
        }

        if (request.Outcome == ReleaseOutcome.Cancelled)
        {
            throw new BusinessRuleException("Use the Cancel action to cancel a release.");
        }

        release.RecordOutcome(request.Outcome, request.Notes, _clock.UtcNow);

        if (request.Outcome is ReleaseOutcome.Failed or ReleaseOutcome.Remediated or ReleaseOutcome.RolledBack)
        {
            await EnsureReviewAsync(
                release,
                request.Outcome == ReleaseOutcome.Failed ? PirTriggers.FailedDeployment : PirTriggers.RollbackOrRemediation,
                cancellationToken);
        }

        await _audit.WriteAsync(
            "Release.Outcome",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { request.Outcome, request.Notes },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostImplementationReview?> EnsureReviewAsync(
        Release release,
        PirTriggers extraTriggers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        var validation = await _dbContext.PostReleaseValidations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

        var triggers = ReleaseReadinessRules.GetPirTriggers(release, validation) | extraTriggers;
        if (triggers == PirTriggers.None)
        {
            return null;
        }

        var now = _clock.UtcNow;
        var review = _dbContext.PostImplementationReviews.Local.FirstOrDefault(item => item.ReleaseId == release.Id)
                     ?? await _dbContext.PostImplementationReviews
                         .Include(item => item.Actions)
                         .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

        if (review is null)
        {
            review = new PostImplementationReview(Guid.NewGuid(), release.Id, triggers, _currentUser.UserId, now);
            _dbContext.PostImplementationReviews.Add(review);

            await _notifications.CreateForRoleAsync(
                RoleNames.ReleaseManager,
                release.Id,
                $"Release {release.ReleaseNumber}: Post-Release Review required",
                $"Triggers: {triggers}. Record root cause, lessons learned and corrective actions.",
                NotificationType.ActionRequired,
                cancellationToken);
        }
        else if ((review.Triggers & triggers) != triggers && review.Status != PirStatus.Completed)
        {
            review.AddTriggers(triggers, now);
        }

        return review;
    }

    public async Task<Guid> OpenManualReviewAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);
        EnsureRole("Release Manager", RoleNames.ReleaseManager);

        var release = await LoadReleaseAsync(releaseId, cancellationToken);
        var review = await EnsureReviewAsync(release, PirTriggers.Manual, cancellationToken)
                     ?? throw new BusinessRuleException("Unable to open the review.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        return review.Id;
    }

    public async Task UpdateReviewAsync(UpdatePirRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRole("Release Manager", RoleNames.ReleaseManager, RoleNames.TechnicalOwner);

        var review = await LoadReviewAsync(request.ReviewId, cancellationToken);

        try
        {
            review.Update(request.Format, request.RootCause, request.LessonsLearned, request.BacklogReference, _clock.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        await _audit.WriteAsync("PIR.Update", nameof(PostImplementationReview), review.Id.ToString(), null, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddActionAsync(AddPirActionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRole("Release Manager", RoleNames.ReleaseManager, RoleNames.TechnicalOwner);

        if (string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.OwnerName))
        {
            throw new BusinessRuleException("Action description and owner are required.");
        }

        var review = await LoadReviewAsync(request.ReviewId, cancellationToken);
        var action = new PirAction(
            Guid.NewGuid(),
            review.Id,
            request.Description,
            request.OwnerName,
            DateTime.SpecifyKind(request.TargetDateUtc, DateTimeKind.Utc),
            request.Reference);

        try
        {
            review.AddAction(action);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        _dbContext.PirActions.Add(action);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteActionAsync(Guid reviewId, Guid actionId, CancellationToken cancellationToken = default)
    {
        EnsureRole("Release Manager", RoleNames.ReleaseManager, RoleNames.TechnicalOwner);

        var review = await LoadReviewAsync(reviewId, cancellationToken);
        var action = review.Actions.SingleOrDefault(item => item.Id == actionId)
                     ?? throw new NotFoundException(nameof(PirAction), actionId);

        action.MarkCompleted(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        EnsureRole("Release Manager", RoleNames.ReleaseManager);

        var review = await LoadReviewAsync(reviewId, cancellationToken);

        try
        {
            review.Complete(_currentUser.UserId, _clock.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        var release = await _dbContext.Releases
            .Include(item => item.References)
            .SingleOrDefaultAsync(item => item.Id == review.ReleaseId, cancellationToken);

        if (release is not null &&
            !release.References.Any(item => item.ReferenceType == ReleaseReferenceType.PostImplementationReview))
        {
            var now = _clock.UtcNow;
            var reference = new ReleaseReference(
                Guid.NewGuid(),
                release.Id,
                ReleaseReferenceType.PostImplementationReview,
                review.Id.ToString("N")[..8].ToUpperInvariant(),
                null,
                "Post-Release Review (platform)",
                _currentUser.UserId,
                now);
            release.AddReference(reference, now);
            _dbContext.ReleaseReferences.Add(reference);
        }

        await _audit.WriteAsync("PIR.Complete", nameof(PostImplementationReview), review.Id.ToString(), null, null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostImplementationReviewDto?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.PostImplementationReviews
            .AsNoTracking()
            .Include(item => item.Actions)
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);

        if (review is null)
        {
            return null;
        }

        await _authorization.EnsureCanViewAsync(review.ReleaseId, cancellationToken);

        var release = await _dbContext.Releases
            .AsNoTracking()
            .Where(item => item.Id == review.ReleaseId)
            .Select(item => new { item.ReleaseNumber, item.Title })
            .SingleAsync(cancellationToken);

        return Map(review, release.ReleaseNumber, release.Title);
    }

    public async Task<IReadOnlyCollection<PostImplementationReviewDto>> ListReviewsAsync(CancellationToken cancellationToken = default)
    {
        var reviews = await _dbContext.PostImplementationReviews
            .AsNoTracking()
            .Include(item => item.Actions)
            .OrderByDescending(item => item.CreatedDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        var releaseIds = reviews.Select(item => item.ReleaseId).Distinct().ToArray();
        var releases = await _dbContext.Releases
            .AsNoTracking()
            .Where(item => releaseIds.Contains(item.Id))
            .Select(item => new { item.Id, item.ReleaseNumber, item.Title })
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return reviews
            .Select(review =>
            {
                var release = releases.GetValueOrDefault(review.ReleaseId);
                return Map(review, release?.ReleaseNumber ?? string.Empty, release?.Title ?? string.Empty);
            })
            .ToArray();
    }

    public static PostImplementationReviewDto Map(PostImplementationReview review, string releaseNumber, string releaseTitle) =>
        new()
        {
            Id = review.Id,
            ReleaseId = review.ReleaseId,
            ReleaseNumber = releaseNumber,
            ReleaseTitle = releaseTitle,
            Triggers = review.Triggers,
            Status = review.Status,
            Format = review.Format,
            RootCause = review.RootCause,
            LessonsLearned = review.LessonsLearned,
            BacklogReference = review.BacklogReference,
            CreatedDate = review.CreatedDate,
            CompletedDate = review.CompletedDate,
            Actions = review.Actions
                .OrderBy(item => item.TargetDate)
                .Select(item => new PirActionDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    OwnerName = item.OwnerName,
                    TargetDate = item.TargetDate,
                    Reference = item.Reference,
                    IsCompleted = item.IsCompleted,
                    CompletedDate = item.CompletedDate
                })
                .ToArray()
        };

    private async Task<PostReleaseValidation> GetOrCreateValidationAsync(
        Release release,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var validation = await _dbContext.PostReleaseValidations
            .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

        if (validation is null)
        {
            validation = new PostReleaseValidation(
                Guid.NewGuid(),
                release.Id,
                ReleaseReadinessRules.RequiresBusinessValidation(release),
                now);
            _dbContext.PostReleaseValidations.Add(validation);
        }

        return validation;
    }

    private async Task<Release> LoadReleaseAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        var release = await _dbContext.Releases
            .Include(item => item.Services)
            .Include(item => item.References)
            .Include(item => item.DeploymentRecords)
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken);

        return release ?? throw new NotFoundException(nameof(Release), releaseId);
    }

    private async Task<PostImplementationReview> LoadReviewAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await _dbContext.PostImplementationReviews
            .Include(item => item.Actions)
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException(nameof(PostImplementationReview), reviewId);
        }

        await _authorization.EnsureCanViewAsync(review.ReleaseId, cancellationToken);
        return review;
    }

    private static void EnsurePostDeployment(Release release)
    {
        if (release.CurrentStatus is not (ReleaseStatus.Deployed or ReleaseStatus.Stabilization))
        {
            throw new ConflictException(
                $"Post-release validation can only be recorded after deployment (current status: {release.CurrentStatus}).");
        }
    }

    private void EnsureRole(string label, params string[] roles)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator) || roles.Any(_currentUser.IsInRole))
        {
            return;
        }

        throw new ForbiddenException($"Only the {label} can perform this action.");
    }
}
