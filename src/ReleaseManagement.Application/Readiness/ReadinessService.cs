using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.Readiness;

/// <summary>
/// Procedure v4.0 §5 / §1.3 / §6.4 — link-first evidence, readiness controls and
/// stakeholder communication on the Release Record.
/// </summary>
public interface IReadinessService
{
    Task AddReferenceAsync(AddReleaseReferenceRequest request, CancellationToken cancellationToken = default);

    Task RemoveReferenceAsync(Guid releaseId, Guid referenceId, CancellationToken cancellationToken = default);

    Task SetControlAsync(SetReadinessControlRequest request, CancellationToken cancellationToken = default);

    Task LogCommunicationAsync(LogCommunicationRequest request, CancellationToken cancellationToken = default);

    Task RescheduleAsync(RescheduleReleaseRequest request, CancellationToken cancellationToken = default);

    Task AssignOwnersAsync(
        Guid releaseId,
        Guid? technicalOwnerUserId,
        Guid? releaseManagerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates / refreshes the control rows for the release (idempotent). Does not save.</summary>
    void EnsureControls(Release release, DateTime nowUtc);

    /// <summary>Releases where the current user's role owns a pending required control.</summary>
    Task<IReadOnlyCollection<Guid>> GetReleasesAwaitingMyReadinessAsync(CancellationToken cancellationToken = default);
}

public sealed class ReadinessService : IReadinessService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public ReadinessService(
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

    public async Task AddReferenceAsync(
        AddReleaseReferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            throw new BusinessRuleException("Reference ID is required.");
        }

        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);
        EnsureNotTerminal(release);
        EnsureRecordEditor(release);

        var now = _clock.UtcNow;
        var reference = new ReleaseReference(
            Guid.NewGuid(),
            release.Id,
            request.ReferenceType,
            request.ExternalId,
            request.Url,
            request.Title,
            _currentUser.UserId,
            now);

        try
        {
            release.AddReference(reference, now);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        _dbContext.ReleaseReferences.Add(reference);

        await _audit.WriteAsync(
            "Release.AddReference",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { request.ReferenceType, request.ExternalId, request.Url },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReferenceAsync(
        Guid releaseId,
        Guid referenceId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        var release = await LoadReleaseAsync(releaseId, cancellationToken);
        EnsureNotTerminal(release);
        EnsureRecordEditor(release);

        var reference = release.References.SingleOrDefault(item => item.Id == referenceId);
        if (reference is null)
        {
            throw new NotFoundException(nameof(ReleaseReference), referenceId);
        }

        release.RemoveReference(referenceId, _clock.UtcNow);
        _dbContext.ReleaseReferences.Remove(reference);

        await _audit.WriteAsync(
            "Release.RemoveReference",
            nameof(Release),
            release.Id.ToString(),
            new { reference.ReferenceType, reference.ExternalId },
            null,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetControlAsync(
        SetReadinessControlRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);

        if (release.CurrentStatus is not (
            ReleaseStatus.ReadinessInProgress or
            ReleaseStatus.ReadyForRelease or
            ReleaseStatus.ReleaseManagerReview))
        {
            throw new ConflictException(
                $"Readiness controls can only be updated while the release is in readiness (current status: {release.CurrentStatus}).");
        }

        var definition = ReleaseReadinessRules.GetDefinition(request.ControlType);
        if (!ReleaseReadinessRules.CanUserSetControl(definition, _currentUser.Roles))
        {
            throw new ForbiddenException(
                $"Only {string.Join(" / ", definition.AllowedRoles)} can set '{definition.Title}'.");
        }

        if (request.Status == ReadinessControlStatus.ReadyWithApprovedException &&
            request.ControlType != ReadinessControlType.SecurityReadiness)
        {
            throw new BusinessRuleException(
                "'Ready with Approved Exception' is only defined for security readiness (§5.2).");
        }

        var now = _clock.UtcNow;
        EnsureControls(release, now);

        var control = release.ReadinessControls.Single(item => item.ControlType == request.ControlType);

        if (request.ControlType == ReadinessControlType.SecurityReadiness &&
            request.Status == ReadinessControlStatus.NotRequired &&
            release.SecurityTriggers != SecurityTriggers.None)
        {
            throw new BusinessRuleException(
                "Security readiness cannot be Not Required while pentest / assessment triggers are selected (§5.2).");
        }

        var previous = control.Status;

        try
        {
            control.SetStatus(
                request.Status,
                request.EvidenceReference,
                request.Justification,
                _currentUser.UserId,
                now);
        }
        catch (ArgumentException exception)
        {
            throw new BusinessRuleException(exception.Message);
        }

        await _audit.WriteAsync(
            "Release.ReadinessControl",
            nameof(Release),
            release.Id.ToString(),
            new { request.ControlType, Status = previous },
            new { request.ControlType, request.Status, request.EvidenceReference, request.Justification },
            cancellationToken);

        if (request.Status == ReadinessControlStatus.Blocked)
        {
            await _notifications.CreateForRoleAsync(
                RoleNames.ReleaseManager,
                release.Id,
                $"Release {release.ReleaseNumber}: {definition.Title} blocked",
                request.Justification ?? "Readiness control blocked.",
                NotificationType.Warning,
                cancellationToken);
        }
        else if (release.ReadinessControls.All(item => item.IsClosed))
        {
            await _notifications.CreateForRoleAsync(
                RoleNames.ReleaseManager,
                release.Id,
                $"Release {release.ReleaseNumber}: all readiness controls closed",
                "Every applicable readiness control is Ready or Not Required. The release can be marked ready.",
                NotificationType.ActionRequired,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogCommunicationAsync(
        LogCommunicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Audience) ||
            string.IsNullOrWhiteSpace(request.Channel) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            throw new BusinessRuleException("Audience, channel and message are required.");
        }

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);
        EnsureRecordEditor(release);

        var now = _clock.UtcNow;
        var communication = new ReleaseCommunication(
            Guid.NewGuid(),
            release.Id,
            request.CommunicationType,
            request.Audience,
            request.Channel,
            request.Message,
            _currentUser.UserId,
            now);

        release.AddCommunication(communication, now);
        _dbContext.ReleaseCommunications.Add(communication);

        await _audit.WriteAsync(
            "Release.Communication",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { request.CommunicationType, request.Audience, request.Channel },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RescheduleAsync(
        RescheduleReleaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureReleaseManager();
        await _authorization.EnsureCanViewAsync(request.ReleaseId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleException("A reason is required to reschedule a release (§3.2).");
        }

        var release = await LoadReleaseAsync(request.ReleaseId, cancellationToken);
        var now = _clock.UtcNow;
        var previous = new { release.PlannedWindowStart, release.PlannedWindowEnd };

        try
        {
            release.Reschedule(request.PlannedWindowStartUtc, request.PlannedWindowEndUtc, now);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            throw new BusinessRuleException(exception.Message);
        }

        if (request.NotifyStakeholders)
        {
            var communication = new ReleaseCommunication(
                Guid.NewGuid(),
                release.Id,
                CommunicationType.Reschedule,
                string.IsNullOrWhiteSpace(request.Audience) ? "Stakeholders" : request.Audience,
                "Platform",
                $"Window moved to {request.PlannedWindowStartUtc:u} – {request.PlannedWindowEndUtc:u}. Reason: {request.Reason}",
                _currentUser.UserId,
                now);

            release.AddCommunication(communication, now);
            _dbContext.ReleaseCommunications.Add(communication);
        }

        release.AddComment(
            new ReleaseComment(
                Guid.NewGuid(),
                release.Id,
                _currentUser.UserId,
                $"Rescheduled to {request.PlannedWindowStartUtc:u} – {request.PlannedWindowEndUtc:u}: {request.Reason}",
                CommentType.System,
                isInternal: false,
                parentCommentId: null,
                now));

        await _notifications.CreateAsync(
            release.CreatedByUserId,
            release.Id,
            $"Release {release.ReleaseNumber} rescheduled",
            $"New window: {request.PlannedWindowStartUtc:u} – {request.PlannedWindowEndUtc:u}. {request.Reason}",
            NotificationType.Information,
            cancellationToken);

        await _audit.WriteAsync(
            "Release.Reschedule",
            nameof(Release),
            release.Id.ToString(),
            previous,
            new { release.PlannedWindowStart, release.PlannedWindowEnd, request.Reason },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignOwnersAsync(
        Guid releaseId,
        Guid? technicalOwnerUserId,
        Guid? releaseManagerUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureReleaseManager();
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        var release = await LoadReleaseAsync(releaseId, cancellationToken);
        EnsureNotTerminal(release);

        release.AssignOwners(technicalOwnerUserId, releaseManagerUserId, _clock.UtcNow);

        await _audit.WriteAsync(
            "Release.AssignOwners",
            nameof(Release),
            release.Id.ToString(),
            null,
            new { technicalOwnerUserId, releaseManagerUserId },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void EnsureControls(Release release, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(release);

        foreach (var definition in ReleaseReadinessRules.Definitions)
        {
            var required = ReleaseReadinessRules.IsControlRequired(release, definition.ControlType);
            var existing = release.ReadinessControls.SingleOrDefault(item => item.ControlType == definition.ControlType);

            if (existing is null)
            {
                var control = new ReadinessControl(
                    Guid.NewGuid(),
                    release.Id,
                    definition.ControlType,
                    definition.OwnerRole,
                    required,
                    nowUtc);

                release.AddReadinessControl(control);
                _dbContext.ReadinessControls.Add(control);
            }
            else if (existing.IsRequired != required)
            {
                existing.SetRequired(required, nowUtc);
            }
        }
    }

    public async Task<IReadOnlyCollection<Guid>> GetReleasesAwaitingMyReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return [];
        }

        var roles = _currentUser.Roles.ToArray();
        var isAdmin = _currentUser.IsInRole(RoleNames.Administrator);

        var controlTypes = ReleaseReadinessRules.Definitions
            .Where(definition => isAdmin ||
                                 definition.AllowedRoles.Any(role => roles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            .Select(definition => definition.ControlType)
            .ToArray();

        if (controlTypes.Length == 0)
        {
            return [];
        }

        var releaseIds = await _dbContext.ReadinessControls
            .AsNoTracking()
            .Where(control =>
                control.IsRequired &&
                control.Status == ReadinessControlStatus.Pending &&
                controlTypes.Contains(control.ControlType))
            .Select(control => control.ReleaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (releaseIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Releases
            .AsNoTracking()
            .Where(release =>
                releaseIds.Contains(release.Id) &&
                release.CurrentStatus == ReleaseStatus.ReadinessInProgress)
            .Select(release => release.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<Release> LoadReleaseAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        var release = await _dbContext.Releases
            .Include(item => item.Services)
            .Include(item => item.References)
            .Include(item => item.ReadinessControls)
            .Include(item => item.Communications)
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(Release), releaseId);
        }

        return release;
    }

    private void EnsureRecordEditor(Release release)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.ReleaseManager) ||
            _currentUser.IsInRole(RoleNames.TechnicalOwner) ||
            release.CreatedByUserId == _currentUser.UserId ||
            release.TechnicalOwnerUserId == _currentUser.UserId)
        {
            return;
        }

        throw new ForbiddenException(
            "Only the Product Owner, Technical Owner or Release Manager can edit the Release Record links and communication log.");
    }

    private void EnsureReleaseManager()
    {
        if (_currentUser.IsInRole(RoleNames.Administrator) ||
            _currentUser.IsInRole(RoleNames.ReleaseManager))
        {
            return;
        }

        throw new ForbiddenException("Only the Release Manager can perform this action.");
    }

    private static void EnsureNotTerminal(Release release)
    {
        if (ReleaseWorkflowRules.IsTerminal(release.CurrentStatus))
        {
            throw new ConflictException(
                $"Release '{release.ReleaseNumber}' is {release.CurrentStatus} and can no longer be changed.");
        }
    }
}
