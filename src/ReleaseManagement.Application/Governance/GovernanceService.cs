using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Application.Governance;

/// <summary>Procedure v4.0 §9 KPIs and Appendix A (CBAR 7.28) evidence mapping.</summary>
public interface IGovernanceService
{
    Task<IReadOnlyCollection<MonthlyKpiDto>> GetMonthlyKpisAsync(int months, CancellationToken cancellationToken = default);

    Task<CompliancePackDto> GetCompliancePackAsync(Guid releaseId, CancellationToken cancellationToken = default);
}

public sealed class GovernanceService : IGovernanceService
{
    private static readonly string[] PlannedModeEvidence = ["Planned execution mode"];

    private readonly IApplicationDbContext _dbContext;
    private readonly IReleaseAuthorizationService _authorization;
    private readonly IClock _clock;

    public GovernanceService(IApplicationDbContext dbContext, IReleaseAuthorizationService authorization, IClock clock)
    {
        _dbContext = dbContext;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<MonthlyKpiDto>> GetMonthlyKpisAsync(
        int months,
        CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 24);
        var now = _clock.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

        var releases = await _dbContext.Releases
            .AsNoTracking()
            .Where(item =>
                (item.ActualReleaseDate != null && item.ActualReleaseDate >= firstMonth) ||
                (item.ClosedDate != null && item.ClosedDate >= firstMonth))
            .Select(item => new
            {
                item.Id,
                item.CurrentStatus,
                item.Outcome,
                item.ExecutionMode,
                item.ActualReleaseDate,
                item.ActualWindowStart,
                item.ActualWindowEnd,
                item.PlannedWindowStart,
                item.PlannedWindowEnd,
                item.ClosedDate
            })
            .ToListAsync(cancellationToken);

        var releaseIds = releases.Select(item => item.Id).ToArray();

        var incidentReleaseIds = await _dbContext.ReleaseReferences
            .AsNoTracking()
            .Where(item => releaseIds.Contains(item.ReleaseId) && item.ReferenceType == ReleaseReferenceType.Incident)
            .Select(item => item.ReleaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var openPirReleaseIds = await _dbContext.PostImplementationReviews
            .AsNoTracking()
            .Where(item => releaseIds.Contains(item.ReleaseId) && item.Status != PirStatus.Completed)
            .Select(item => item.ReleaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<MonthlyKpiDto>();

        for (var offset = months - 1; offset >= 0; offset--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset);
            var monthEnd = monthStart.AddMonths(1);

            var deployed = releases
                .Where(item => item.ActualReleaseDate >= monthStart && item.ActualReleaseDate < monthEnd)
                .ToArray();

            var completed = releases
                .Where(item =>
                    item.Outcome != null &&
                    item.Outcome != ReleaseOutcome.Cancelled &&
                    ((item.ClosedDate >= monthStart && item.ClosedDate < monthEnd) ||
                     (item.ClosedDate == null && item.ActualReleaseDate >= monthStart && item.ActualReleaseDate < monthEnd)))
                .ToArray();

            result.Add(new MonthlyKpiDto
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                CompletedReleases = completed.Length,
                SuccessfulReleases = completed.Count(item => item.Outcome == ReleaseOutcome.Successful),
                FailedReleases = completed.Count(item => item.Outcome == ReleaseOutcome.Failed),
                RolledBackOrRemediated = deployed.Count(item =>
                    item.Outcome is ReleaseOutcome.RolledBack or ReleaseOutcome.Remediated ||
                    item.CurrentStatus is ReleaseStatus.RolledBack or ReleaseStatus.RollbackInProgress),
                ReleasesWithIncidents = deployed.Count(item => incidentReleaseIds.Contains(item.Id)),
                DeployedReleases = deployed.Length,
                DeployedWithinWindow = deployed.Count(item =>
                    item.ActualWindowStart >= item.PlannedWindowStart.AddMinutes(-15) &&
                    (item.ActualWindowEnd ?? item.ActualReleaseDate) <= item.PlannedWindowEnd.AddMinutes(15)),
                ExpeditedReleases = deployed.Count(item => item.ExecutionMode == ExecutionMode.Expedited),
                OpenPirs = deployed.Count(item => openPirReleaseIds.Contains(item.Id))
            });
        }

        return result;
    }

    public async Task<CompliancePackDto> GetCompliancePackAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanViewAsync(releaseId, cancellationToken);

        var release = await _dbContext.Releases
            .AsNoTracking()
            .Include(item => item.Services)
            .Include(item => item.References)
            .Include(item => item.ReadinessControls)
            .Include(item => item.Communications)
            .Include(item => item.DeploymentRecords)
            .Include(item => item.StatusHistory)
            .SingleOrDefaultAsync(item => item.Id == releaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Release), releaseId);

        var validation = await _dbContext.PostReleaseValidations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReleaseId == releaseId, cancellationToken);

        var review = await _dbContext.PostImplementationReviews
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReleaseId == releaseId, cancellationToken);

        var forecast = release.ForecastId.HasValue
            ? await _dbContext.ReleaseForecasts.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == release.ForecastId.Value, cancellationToken)
            : null;

        var items = new List<ComplianceItemDto>();

        // 7.28.1 — registration
        var sourceRefs = release.References.Where(item => item.IsSourceReference).ToArray();
        items.Add(Item(
            "7.28.1",
            "Release registration: Release ID and links to source work items / change.",
            sourceRefs.Length > 0,
            new[] { $"Release ID {release.ReleaseNumber}" }.Concat(sourceRefs.Select(FormatReference)).ToArray(),
            "No source reference (WI / PR / pipeline / artifact / Change) is linked."));

        // 7.28.2 — planning and testing
        var qa = Control(release, ReadinessControlType.QaTechnicalValidation);
        var planningEvidence = new List<string>
        {
            $"Planned window {release.PlannedWindowStart:u} – {release.PlannedWindowEnd:u}",
            $"Category {release.Category}, mode {release.ExecutionMode}"
        };
        if (forecast is not null)
        {
            planningEvidence.Add($"Forecast Q{forecast.Quarter} {forecast.Year}: {forecast.Title}");
        }

        if (qa is not null)
        {
            planningEvidence.Add(FormatControl(qa));
        }

        items.Add(Item(
            "7.28.2",
            "Planning and testing: forecast / calendar entry and QA readiness evidence.",
            qa is { IsClosed: true },
            planningEvidence,
            "QA / technical validation control is not closed."));

        // 7.28.3 — impact and security risk assessment
        var security = Control(release, ReadinessControlType.SecurityReadiness);
        items.Add(Item(
            "7.28.3",
            "Impact and security risk assessment: classification criteria and security readiness status.",
            security is { IsClosed: true },
            new[]
            {
                $"Classification criteria: {(release.ClassificationCriteria == ClassificationCriteria.None ? "none" : release.ClassificationCriteria.ToString())}",
                $"Security triggers: {(release.SecurityTriggers == SecurityTriggers.None ? "none" : release.SecurityTriggers.ToString())}",
                security is null ? "Security readiness not initialised" : FormatControl(security)
            },
            "Security readiness control is not closed."));

        // 7.28.4 — security verification
        var securityRefs = release.References.Where(item => item.ReferenceType == ReleaseReferenceType.SecurityAssessment).ToArray();
        var securityVerified = security is { Status: ReadinessControlStatus.Ready or ReadinessControlStatus.ReadyWithApprovedException }
                               || security is { Status: ReadinessControlStatus.NotRequired } && release.SecurityTriggers == SecurityTriggers.None;
        items.Add(Item(
            "7.28.4",
            "Security verification: pentest / assessment evidence or an approved exception.",
            securityVerified,
            security is null
                ? Array.Empty<string>()
                : new[] { FormatControl(security) }.Concat(securityRefs.Select(FormatReference)).ToArray(),
            "No security verification evidence or approved exception."));

        // 7.28.5 — stakeholder communication
        var requiresComms = ReleaseReadinessRules.RequiresPreReleaseCommunication(release);
        items.Add(Item(
            "7.28.5",
            "Stakeholder communication proportional to impact.",
            !requiresComms || release.Communications.Any(item => item.CommunicationType == CommunicationType.PreRelease),
            release.Communications
                .OrderBy(item => item.SentDate)
                .Select(item => $"{item.SentDate:u} {item.CommunicationType} → {item.Audience} via {item.Channel}")
                .ToArray(),
            requiresComms ? "Impact requires a pre-release notification but none is logged." : null));

        // 7.28.6 — rollback / unexpected results
        var recovery = Control(release, ReadinessControlType.RecoveryReadiness);
        var recoveryEvidence = new List<string>
        {
            $"Recovery approach: {release.RecoveryApproach}"
        };
        if (recovery is not null)
        {
            recoveryEvidence.Add(FormatControl(recovery));
        }

        recoveryEvidence.AddRange(release.DeploymentRecords
            .OrderBy(item => item.StartedDate)
            .Select(item => $"Deployment {item.StartedDate:u}: {item.Status}{(item.RollbackRequired ? " (rollback required)" : string.Empty)}"));

        if (validation is not null)
        {
            recoveryEvidence.Add($"Post-release validation: technical {validation.TechnicalResult}, business {(validation.BusinessValidationRequired ? validation.BusinessResult.ToString() : "n/a")}");
        }

        if (release.Outcome is not null)
        {
            recoveryEvidence.Add($"Outcome: {release.Outcome}");
        }

        items.Add(Item(
            "7.28.6",
            "Rollback and handling of unexpected results: recovery readiness, validation and outcome.",
            recovery is { IsClosed: true },
            recoveryEvidence,
            "Recovery readiness control is not closed."));

        // 7.28.7 — urgent / expedited
        var expeditedOk = !release.IsExpedited ||
                          (!string.IsNullOrWhiteSpace(release.ExpeditedJustification) &&
                           (review is null || review.Status == PirStatus.Completed || release.CurrentStatus != ReleaseStatus.Closed));
        items.Add(Item(
            "7.28.7",
            "Urgent / expedited releases: justification, authority and mandatory PIR.",
            expeditedOk,
            release.IsExpedited
                ? new[]
                {
                    $"Justification: {release.ExpeditedJustification}",
                    $"Director authorization: {release.DirectorApprovalReference ?? "—"}",
                    review is null ? "PIR: not opened" : $"PIR: {review.Status}"
                }
                : PlannedModeEvidence,
            release.IsExpedited ? "Expedited release without completed justification / PIR." : null));

        // 7.28.8 — retention / reconstructability
        var historyCount = release.StatusHistory.Count;
        items.Add(Item(
            "7.28.8",
            "Retention: the record, status history, readiness, deployment, validation and PIR evidence are retained ≥ 5 years (retention policy is an internal control outside the platform).",
            historyCount > 0,
            new[]
            {
                $"{historyCount} status history entries",
                $"{release.ReadinessControls.Count} readiness controls, {release.References.Count} references, {release.Communications.Count} communications",
                "Retention/legal hold is governed by IT Governance policy (not enforced by the platform)."
            },
            null));

        return new CompliancePackDto
        {
            ReleaseId = release.Id,
            ReleaseNumber = release.ReleaseNumber,
            Title = release.Title,
            GeneratedAtUtc = _clock.UtcNow,
            Items = items
        };
    }

    private static ComplianceItemDto Item(
        string clause,
        string requirement,
        bool satisfied,
        IReadOnlyCollection<string> evidence,
        string? gap) =>
        new()
        {
            Clause = clause,
            Requirement = requirement,
            IsSatisfied = satisfied,
            Evidence = evidence,
            Gap = satisfied ? null : gap
        };

    private static ReadinessControl? Control(Release release, ReadinessControlType type) =>
        release.ReadinessControls.SingleOrDefault(item => item.ControlType == type);

    private static string FormatControl(ReadinessControl control) =>
        $"{control.ControlType}: {control.Status}{(string.IsNullOrWhiteSpace(control.EvidenceReference) ? string.Empty : $" — {control.EvidenceReference}")}{(string.IsNullOrWhiteSpace(control.Justification) ? string.Empty : $" ({control.Justification})")}";

    private static string FormatReference(ReleaseReference reference) =>
        $"{reference.ReferenceType} {reference.ExternalId}{(string.IsNullOrWhiteSpace(reference.Url) ? string.Empty : $" — {reference.Url}")}";
}
