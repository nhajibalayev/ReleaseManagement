using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.DTOs.Releases;

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
}
