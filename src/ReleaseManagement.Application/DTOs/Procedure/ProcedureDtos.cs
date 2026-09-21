using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.DTOs.Procedure;

// ---------------------------------------------------------------- references

public sealed class ReleaseReferenceDto
{
    public Guid Id { get; init; }

    public ReleaseReferenceType ReferenceType { get; init; }

    public string ExternalId { get; init; } = string.Empty;

    public string? Url { get; init; }

    public string? Title { get; init; }

    public DateTime AddedDate { get; init; }

    public bool IsSourceReference { get; init; }
}

public sealed class AddReleaseReferenceRequest
{
    public Guid ReleaseId { get; set; }

    public ReleaseReferenceType ReferenceType { get; set; } = ReleaseReferenceType.WorkItem;

    public string ExternalId { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Title { get; set; }
}

// ---------------------------------------------------------------- readiness

public sealed class ReadinessControlDto
{
    public Guid? Id { get; init; }

    public ReadinessControlType ControlType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ProcedureSection { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string OwnerRole { get; init; } = string.Empty;

    public bool IsRequired { get; init; }

    public ReadinessControlStatus Status { get; init; }

    public string? EvidenceReference { get; init; }

    public string? Justification { get; init; }

    public string? UpdatedByDisplay { get; init; }

    public DateTime? UpdatedDate { get; init; }

    public bool CanCurrentUserSet { get; init; }

    public bool IsClosed { get; init; }
}

public sealed class SetReadinessControlRequest
{
    public Guid ReleaseId { get; set; }

    public ReadinessControlType ControlType { get; set; }

    public ReadinessControlStatus Status { get; set; }

    public string? EvidenceReference { get; set; }

    public string? Justification { get; set; }
}

// ---------------------------------------------------------------- communication

public sealed class ReleaseCommunicationDto
{
    public Guid Id { get; init; }

    public CommunicationType CommunicationType { get; init; }

    public string Audience { get; init; } = string.Empty;

    public string Channel { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? SentByDisplay { get; init; }

    public DateTime SentDate { get; init; }
}

public sealed class LogCommunicationRequest
{
    public Guid ReleaseId { get; set; }

    public CommunicationType CommunicationType { get; set; } = CommunicationType.PreRelease;

    public string Audience { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class RescheduleReleaseRequest
{
    public Guid ReleaseId { get; set; }

    public DateTime PlannedWindowStartUtc { get; set; }

    public DateTime PlannedWindowEndUtc { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool NotifyStakeholders { get; set; }

    public string? Audience { get; set; }
}

// ---------------------------------------------------------------- post-release

public sealed class PostReleaseValidationDto
{
    public bool Exists { get; init; }

    public ValidationResult TechnicalResult { get; init; }

    public bool HealthCheckPassed { get; init; }

    public bool SmokeTestPassed { get; init; }

    public bool MonitoringClean { get; init; }

    public bool RecoveryNeeded { get; init; }

    public string? TechnicalNotes { get; init; }

    public string? TechnicalEvidenceReference { get; init; }

    public DateTime? TechnicalValidatedDate { get; init; }

    public string? TechnicalValidatedByDisplay { get; init; }

    public bool BusinessValidationRequired { get; init; }

    public ValidationResult BusinessResult { get; init; }

    public string? BusinessNotes { get; init; }

    public DateTime? BusinessValidatedDate { get; init; }

    public string? BusinessValidatedByDisplay { get; init; }

    public bool IsComplete { get; init; }

    public bool IsPassed { get; init; }
}

public sealed class RecordTechnicalValidationRequest
{
    public Guid ReleaseId { get; set; }

    public ValidationResult Result { get; set; } = ValidationResult.Passed;

    public bool HealthCheckPassed { get; set; }

    public bool SmokeTestPassed { get; set; }

    public bool MonitoringClean { get; set; }

    public bool RecoveryNeeded { get; set; }

    public string? Notes { get; set; }

    public string? EvidenceReference { get; set; }
}

public sealed class RecordBusinessValidationRequest
{
    public Guid ReleaseId { get; set; }

    public ValidationResult Result { get; set; } = ValidationResult.Passed;

    public string? Notes { get; set; }
}

public sealed class StartStabilizationRequest
{
    public Guid ReleaseId { get; set; }

    public DateTime StabilizationEndUtc { get; set; }

    public string? Notes { get; set; }
}

public sealed class RecordOutcomeRequest
{
    public Guid ReleaseId { get; set; }

    public ReleaseOutcome Outcome { get; set; } = ReleaseOutcome.Successful;

    public string? Notes { get; set; }
}

// ---------------------------------------------------------------- PIR

public sealed class PirActionDto
{
    public Guid Id { get; init; }

    public string Description { get; init; } = string.Empty;

    public string OwnerName { get; init; } = string.Empty;

    public DateTime TargetDate { get; init; }

    public string? Reference { get; init; }

    public bool IsCompleted { get; init; }

    public DateTime? CompletedDate { get; init; }
}

public sealed class PostImplementationReviewDto
{
    public Guid Id { get; init; }

    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string ReleaseTitle { get; init; } = string.Empty;

    public PirTriggers Triggers { get; init; }

    public PirStatus Status { get; init; }

    public PirFormat Format { get; init; }

    public string? RootCause { get; init; }

    public string? LessonsLearned { get; init; }

    public string? BacklogReference { get; init; }

    public DateTime CreatedDate { get; init; }

    public DateTime? CompletedDate { get; init; }

    public IReadOnlyCollection<PirActionDto> Actions { get; init; } = [];
}

public sealed class UpdatePirRequest
{
    public Guid ReviewId { get; set; }

    public PirFormat Format { get; set; } = PirFormat.AsyncEvidenceReview;

    public string? RootCause { get; set; }

    public string? LessonsLearned { get; set; }

    public string? BacklogReference { get; set; }
}

public sealed class AddPirActionRequest
{
    public Guid ReviewId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public DateTime TargetDateUtc { get; set; }

    public string? Reference { get; set; }
}

// ---------------------------------------------------------------- forecast

public sealed class ReleaseForecastDto
{
    public Guid Id { get; init; }

    public int Year { get; init; }

    public int Quarter { get; init; }

    public Guid ProductId { get; init; }

    public string? ProductName { get; init; }

    public string Title { get; init; } = string.Empty;

    public ReleaseCategory Category { get; init; }

    public string Team { get; init; } = string.Empty;

    public DateTime? ExpectedDate { get; init; }

    public string? Dependencies { get; init; }

    public string? Notes { get; init; }

    public ForecastStatus Status { get; init; }

    public Guid? ReleaseId { get; init; }

    public string? ReleaseNumber { get; init; }
}

public sealed class SaveForecastRequest
{
    public Guid? Id { get; set; }

    public int Year { get; set; }

    public int Quarter { get; set; }

    public Guid ProductId { get; set; }

    public string Title { get; set; } = string.Empty;

    public ReleaseCategory Category { get; set; } = ReleaseCategory.Normal;

    public string Team { get; set; } = string.Empty;

    public DateTime? ExpectedDateUtc { get; set; }

    public string? Dependencies { get; set; }

    public string? Notes { get; set; }

    public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
}

// ---------------------------------------------------------------- freeze

public sealed class FreezePeriodDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public FreezeType FreezeType { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public string Authority { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public int ExceptionCount { get; init; }
}

public sealed class CreateFreezePeriodRequest
{
    public string Name { get; set; } = string.Empty;

    public FreezeType FreezeType { get; set; } = FreezeType.ItTechnical;

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class AddFreezeExceptionRequest
{
    public Guid FreezePeriodId { get; set; }

    public Guid ReleaseId { get; set; }

    public string ApprovedBy { get; set; } = string.Empty;

    public string Justification { get; set; } = string.Empty;
}

public sealed class FreezeConflictDto
{
    public Guid FreezePeriodId { get; init; }

    public string Name { get; init; } = string.Empty;

    public FreezeType FreezeType { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public string Authority { get; init; } = string.Empty;

    public bool HasException { get; init; }
}

// ---------------------------------------------------------------- KPI / compliance

public sealed class MonthlyKpiDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public int CompletedReleases { get; init; }

    public int SuccessfulReleases { get; init; }

    public int FailedReleases { get; init; }

    public int RolledBackOrRemediated { get; init; }

    public int ReleasesWithIncidents { get; init; }

    public int DeployedReleases { get; init; }

    public int DeployedWithinWindow { get; init; }

    public int ExpeditedReleases { get; init; }

    public int OpenPirs { get; init; }

    public double? SuccessRatePercent =>
        CompletedReleases == 0 ? null : SuccessfulReleases * 100d / CompletedReleases;

    public double? IncidentRatePercent =>
        DeployedReleases == 0 ? null : ReleasesWithIncidents * 100d / DeployedReleases;

    public double? RollbackRemediationRatePercent =>
        DeployedReleases == 0 ? null : RolledBackOrRemediated * 100d / DeployedReleases;

    public double? ScheduleAdherencePercent =>
        DeployedReleases == 0 ? null : DeployedWithinWindow * 100d / DeployedReleases;
}

public sealed class ComplianceItemDto
{
    public string Clause { get; init; } = string.Empty;

    public string Requirement { get; init; } = string.Empty;

    public bool IsSatisfied { get; init; }

    public IReadOnlyCollection<string> Evidence { get; init; } = [];

    public string? Gap { get; init; }
}

public sealed class CompliancePackDto
{
    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public IReadOnlyCollection<ComplianceItemDto> Items { get; init; } = [];

    public bool IsFullyMapped => Items.All(item => item.IsSatisfied);
}
