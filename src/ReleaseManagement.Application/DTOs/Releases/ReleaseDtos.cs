using ReleaseManagement.Application.DTOs.Procedure;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.DTOs.Releases;

public sealed class ReleaseReferenceInputDto
{
    public ReleaseReferenceType ReferenceType { get; set; } = ReleaseReferenceType.WorkItem;

    public string ExternalId { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Title { get; set; }
}

public sealed class ReleaseServiceInputDto
{
    public Guid ServiceId { get; set; }

    public string? BranchName { get; set; }

    public string? CommitId { get; set; }

    public string? Version { get; set; }

    public string? BuildNumber { get; set; }

    public string? ArtifactUrl { get; set; }

    public string? RepositoryUrl { get; set; }

    public bool DatabaseChanges { get; set; }

    public bool ConfigurationChanges { get; set; }

    public string? Notes { get; set; }
}

public class CreateReleaseDraftRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public Guid EnvironmentId { get; set; }

    public DateTime PlannedReleaseDateUtc { get; set; }

    public ReleaseType ReleaseType { get; set; } = ReleaseType.Standard;

    public ReleasePriority Priority { get; set; } = ReleasePriority.Medium;

    public string? ReleaseVersion { get; set; }

    public string? BusinessReason { get; set; }

    public string? ImpactDescription { get; set; }

    public string? TestingSummary { get; set; }

    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;

    public string? RiskDescription { get; set; }

    public string? DeploymentPlan { get; set; }

    public string? RollbackPlan { get; set; }

    public string? MonitoringPlan { get; set; }

    public string? PostReleaseValidationPlan { get; set; }

    public bool DowntimeRequired { get; set; }

    public int? ExpectedDowntimeMinutes { get; set; }

    public IList<ReleaseServiceInputDto> Services { get; set; } = [];

    // ---- Procedure v4.0 fields ----

    public ClassificationCriteria ClassificationCriteria { get; set; } = ClassificationCriteria.None;

    public SecurityTriggers SecurityTriggers { get; set; } = SecurityTriggers.None;

    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Planned;

    public string? ExpeditedJustification { get; set; }

    public string? DirectorApprovalReference { get; set; }

    public DateTime PlannedWindowStartUtc { get; set; }

    public DateTime PlannedWindowEndUtc { get; set; }

    public bool PlannedMaintenance { get; set; }

    public string? MaintenanceApprovalReference { get; set; }

    public bool OperationalImpact { get; set; }

    public string? KeyDependencies { get; set; }

    public RecoveryApproach RecoveryApproach { get; set; } = RecoveryApproach.StandardPipelineRollback;

    public string? RecoveryDecisionPoints { get; set; }

    public string? RecoveryResponsibleParties { get; set; }

    public Guid? TechnicalOwnerUserId { get; set; }

    public Guid? ForecastId { get; set; }

    public IList<ReleaseReferenceInputDto> References { get; set; } = [];
}

public sealed class UpdateReleaseDraftRequest : CreateReleaseDraftRequest
{
    public Guid ReleaseId { get; set; }
}

public sealed class TransitionReleaseRequest
{
    public Guid ReleaseId { get; set; }

    public ReleaseStatus TargetStatus { get; set; }

    public string? Comment { get; set; }

    public Guid? AssignedUserId { get; set; }

    /// <summary>§7.2 — used when moving to Stabilization; defaults by category when empty.</summary>
    public DateTime? StabilizationEndUtc { get; set; }

    public string? StabilizationNotes { get; set; }
}

public sealed class ReleaseListItemDto
{
    public Guid Id { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public Guid ProductId { get; init; }

    public string? ProductName { get; init; }

    public string ReleaseVersion { get; init; } = string.Empty;

    public DateTime PlannedReleaseDate { get; init; }

    public ReleaseStatus CurrentStatus { get; init; }

    public string CurrentStatusDisplay { get; init; } = string.Empty;

    public Guid? CurrentResponsibleUserId { get; init; }

    public string? CurrentResponsibleRole { get; init; }

    public string? CurrentResponsibleDisplay { get; init; }

    public DateTime UpdatedDate { get; init; }

    public bool ActionRequiredFromCurrentUser { get; init; }

    public ReleaseCategory Category { get; init; }

    public ExecutionMode ExecutionMode { get; init; }
}

public sealed class ReleaseStatusSummaryDto
{
    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public ReleaseStatus CurrentStatus { get; init; }

    public string CurrentStatusDisplay { get; init; } = string.Empty;

    public string? CurrentResponsibleDisplay { get; init; }

    public DateTime StatusChangedAtUtc { get; init; }

    public string? NextStageDisplay { get; init; }

    public bool ActionRequiredFromCurrentUser { get; init; }

    public string? ReturnOrRejectReason { get; init; }

    public string? ReturnedByDisplay { get; init; }

    public DateTime? ReturnedDateUtc { get; init; }

    public DateTime? PlannedReleaseDateUtc { get; init; }
}

public sealed class ReleaseDetailsDto
{
    public Guid Id { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid ProductId { get; init; }

    public Guid EnvironmentId { get; init; }

    public ReleaseType ReleaseType { get; init; }

    public ReleasePriority Priority { get; init; }

    public string ReleaseVersion { get; init; } = string.Empty;

    public string BusinessReason { get; init; } = string.Empty;

    public string ImpactDescription { get; init; } = string.Empty;

    public string TestingSummary { get; init; } = string.Empty;

    public RiskLevel RiskLevel { get; init; }

    public string RiskDescription { get; init; } = string.Empty;

    public string DeploymentPlan { get; init; } = string.Empty;

    public string RollbackPlan { get; init; } = string.Empty;

    public string MonitoringPlan { get; init; } = string.Empty;

    public string PostReleaseValidationPlan { get; init; } = string.Empty;

    public bool DowntimeRequired { get; init; }

    public int? ExpectedDowntimeMinutes { get; init; }

    public DateTime PlannedReleaseDate { get; init; }

    public DateTime? ActualReleaseDate { get; init; }

    public Guid CreatedByUserId { get; init; }

    public int? AzureDevOpsWorkItemId { get; init; }

    public string? AzureDevOpsWorkItemUrl { get; init; }

    public ReleaseStatusSummaryDto Status { get; init; } = null!;

    public IReadOnlyCollection<ReleaseServiceInputDto> Services { get; init; } = [];

    public IReadOnlyCollection<ReleaseStatus> AllowedTransitions { get; init; } = [];

    // ---- Procedure v4.0 fields ----

    public ReleaseTrack Track { get; init; }

    public ReleaseCategory Category { get; init; }

    public ClassificationCriteria ClassificationCriteria { get; init; }

    public SecurityTriggers SecurityTriggers { get; init; }

    public ExecutionMode ExecutionMode { get; init; }

    public string? ExpeditedJustification { get; init; }

    public string? DirectorApprovalReference { get; init; }

    public Guid? TechnicalOwnerUserId { get; init; }

    public Guid? ReleaseManagerUserId { get; init; }

    public DateTime PlannedWindowStart { get; init; }

    public DateTime PlannedWindowEnd { get; init; }

    public DateTime? ActualWindowStart { get; init; }

    public DateTime? ActualWindowEnd { get; init; }

    public bool PlannedMaintenance { get; init; }

    public string? MaintenanceApprovalReference { get; init; }

    public bool OperationalImpact { get; init; }

    public string KeyDependencies { get; init; } = string.Empty;

    public RecoveryApproach RecoveryApproach { get; init; }

    public string RecoveryDecisionPoints { get; init; } = string.Empty;

    public string RecoveryResponsibleParties { get; init; } = string.Empty;

    public DateTime? StabilizationStart { get; init; }

    public DateTime? StabilizationEnd { get; init; }

    public string? StabilizationNotes { get; init; }

    public ReleaseOutcome? Outcome { get; init; }

    public string? OutcomeNotes { get; init; }

    public Guid? ForecastId { get; init; }

    public IReadOnlyCollection<ReleaseReferenceDto> References { get; init; } = [];

    public IReadOnlyCollection<ReadinessControlDto> ReadinessControls { get; init; } = [];

    public IReadOnlyCollection<ReleaseCommunicationDto> Communications { get; init; } = [];

    public PostReleaseValidationDto Validation { get; init; } = new();

    public PostImplementationReviewDto? Review { get; init; }

    public IReadOnlyCollection<FreezeConflictDto> FreezeConflicts { get; init; } = [];

    /// <summary>Blocking reasons for the next gate (ready / stabilization / closure) — shown to the RM.</summary>
    public IReadOnlyCollection<string> GateErrors { get; init; } = [];

    public bool RequiresPreReleaseCommunication { get; init; }

    public bool RequiresBusinessValidation { get; init; }
}
