using System.ComponentModel.DataAnnotations;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Web.ViewModels;

public sealed class LoginViewModel
{
    [Display(Name = "User name")]
    public string UserName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public bool WindowsAuthEnabled { get; set; }

    public bool ActiveDirectoryLoginEnabled { get; set; }

    public bool AzureAdEnabled { get; set; }

    public bool AllowLocalLogin { get; set; } = true;

    public string? LoginHint { get; set; }
}

public sealed class DashboardViewModel
{
    public int MyOpenReleases { get; init; }

    public int ActionRequired { get; init; }

    public int PendingApprovals { get; init; }

    public int ReadyForRelease { get; init; }

    public int DeployedThisMonth { get; init; }

    public IReadOnlyCollection<ReleaseListItemViewModel> RecentReleases { get; init; } = [];
}

public sealed class ReleaseListItemViewModel
{
    public Guid Id { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? ProductName { get; init; }

    public string ReleaseVersion { get; init; } = string.Empty;

    public DateTime PlannedReleaseDate { get; init; }

    public ReleaseStatus CurrentStatus { get; init; }

    public string CurrentStatusDisplay { get; init; } = string.Empty;

    public string? CurrentResponsibleDisplay { get; init; }

    public DateTime UpdatedDate { get; init; }

    public bool ActionRequiredFromCurrentUser { get; init; }

    public ReleaseCategory Category { get; init; }

    public ExecutionMode ExecutionMode { get; init; }
}

public sealed class ReleaseWizardViewModel
{
    public Guid? ReleaseId { get; set; }

    public int Step { get; set; } = 1;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Product")]
    public Guid ProductId { get; set; }

    [Required]
    [Display(Name = "Environment")]
    public Guid EnvironmentId { get; set; }

    [Required]
    [Display(Name = "Planned release date")]
    [DataType(DataType.Date)]
    public DateTime PlannedReleaseDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    public ReleaseType ReleaseType { get; set; } = ReleaseType.Standard;

    public ReleasePriority Priority { get; set; } = ReleasePriority.Medium;

    [MaxLength(100)]
    [Display(Name = "Release version")]
    public string? ReleaseVersion { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Testing summary")]
    public string? TestingSummary { get; set; }

    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;

    [MaxLength(2000)]
    [Display(Name = "Risk description")]
    public string? RiskDescription { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Deployment plan")]
    public string? DeploymentPlan { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Rollback plan")]
    public string? RollbackPlan { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Monitoring plan")]
    public string? MonitoringPlan { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Post-release validation plan")]
    public string? PostReleaseValidationPlan { get; set; }

    [Display(Name = "Downtime required")]
    public bool DowntimeRequired { get; set; }

    [Display(Name = "Expected downtime (minutes)")]
    public int? ExpectedDowntimeMinutes { get; set; }

    public List<ReleaseServiceRowViewModel> Services { get; set; } = [new()];

    public IReadOnlyCollection<LookupItemViewModel> Products { get; set; } = [];

    public IReadOnlyCollection<LookupItemViewModel> Environments { get; set; } = [];

    public IReadOnlyCollection<LookupItemViewModel> AvailableServices { get; set; } = [];

    // ---- Procedure v4.0 (§3.2 window, §4 classification, §5.3 recovery, §1.3 links) ----

    [Required]
    [Display(Name = "Planned window start")]
    public DateTime PlannedWindowStart { get; set; } = DateTime.UtcNow.Date.AddDays(7).AddHours(20);

    [Required]
    [Display(Name = "Planned window end")]
    public DateTime PlannedWindowEnd { get; set; } = DateTime.UtcNow.Date.AddDays(7).AddHours(22);

    [Display(Name = "Planned maintenance (approved maintenance window)")]
    public bool PlannedMaintenance { get; set; }

    [MaxLength(500)]
    [Display(Name = "Maintenance window approval reference")]
    public string? MaintenanceApprovalReference { get; set; }

    [Display(Name = "Monitoring / support / capacity affected (operational readiness)")]
    public bool OperationalImpact { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Key dependencies")]
    public string? KeyDependencies { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Customer / business impact")]
    public string? ImpactDescription { get; set; }

    [Display(Name = "Technical Owner")]
    public Guid? TechnicalOwnerUserId { get; set; }

    [Display(Name = "Quarterly forecast entry")]
    public Guid? ForecastId { get; set; }

    public List<ClassificationCriteria> Criteria { get; set; } = [];

    public List<SecurityTriggers> Triggers { get; set; } = [];

    [Display(Name = "Execution mode")]
    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Planned;

    [MaxLength(2000)]
    [Display(Name = "Expedited justification (urgency)")]
    public string? ExpeditedJustification { get; set; }

    [MaxLength(500)]
    [Display(Name = "IT Department Director authorization reference")]
    public string? DirectorApprovalReference { get; set; }

    [Display(Name = "Recovery approach")]
    public RecoveryApproach RecoveryApproach { get; set; } = RecoveryApproach.StandardPipelineRollback;

    [MaxLength(4000)]
    [Display(Name = "Recovery decision points (Major)")]
    public string? RecoveryDecisionPoints { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Recovery responsible parties (Major)")]
    public string? RecoveryResponsibleParties { get; set; }

    public List<ReleaseReferenceRowViewModel> References { get; set; } = [new()];

    public IReadOnlyCollection<LookupItemViewModel> TechnicalOwners { get; set; } = [];

    public IReadOnlyCollection<LookupItemViewModel> Forecasts { get; set; } = [];

    public IReadOnlyCollection<string> FreezeWarnings { get; set; } = [];

    public ClassificationCriteria CriteriaFlags =>
        Criteria.Aggregate(ClassificationCriteria.None, (current, item) => current | item);

    public SecurityTriggers TriggerFlags =>
        Triggers.Aggregate(SecurityTriggers.None, (current, item) => current | item);

    public ReleaseCategory PreviewCategory =>
        Domain.Rules.ReleaseClassificationRules.Classify(
            CriteriaFlags,
            Services.Any(service => service.ServiceId != Guid.Empty && service.DatabaseChanges),
            DowntimeRequired);
}

public sealed class ReleaseReferenceRowViewModel
{
    public ReleaseReferenceType ReferenceType { get; set; } = ReleaseReferenceType.WorkItem;

    [MaxLength(200)]
    public string? ExternalId { get; set; }

    [MaxLength(2048)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? Title { get; set; }
}

public sealed class ReleaseServiceRowViewModel
{
    public Guid ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string? BranchName { get; set; }

    public string? CommitId { get; set; }

    public string? Version { get; set; }

    public string? BuildNumber { get; set; }

    public string? RepositoryUrl { get; set; }

    public bool DatabaseChanges { get; set; }

    public bool ConfigurationChanges { get; set; }

    public string? Notes { get; set; }
}

public sealed class LookupItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class ReleaseDetailsPageViewModel
{
    public Guid Id { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ReleasePriority Priority { get; init; }

    public string ReleaseVersion { get; init; } = string.Empty;

    public DateTime PlannedReleaseDate { get; init; }

    public string? ProductName { get; init; }

    public string? EnvironmentName { get; init; }

    public string TestingSummary { get; init; } = string.Empty;

    public RiskLevel RiskLevel { get; init; }

    public string RiskDescription { get; init; } = string.Empty;

    public string DeploymentPlan { get; init; } = string.Empty;

    public string RollbackPlan { get; init; } = string.Empty;

    public string MonitoringPlan { get; init; } = string.Empty;

    public string PostReleaseValidationPlan { get; init; } = string.Empty;

    public bool DowntimeRequired { get; init; }

    public int? ExpectedDowntimeMinutes { get; init; }

    public int? AzureDevOpsWorkItemId { get; init; }

    public string? AzureDevOpsWorkItemUrl { get; init; }

    public ReleaseStatusSummaryViewModel Status { get; init; } = new();

    public IReadOnlyCollection<ReleaseServiceRowViewModel> Services { get; init; } = [];

    public IReadOnlyCollection<ReleaseStatus> AllowedTransitions { get; init; } = [];

    public IReadOnlyCollection<StatusHistoryItemViewModel> History { get; init; } = [];

    public IReadOnlyCollection<CommentItemViewModel> Comments { get; init; } = [];

    public IReadOnlyCollection<AttachmentItemViewModel> Attachments { get; init; } = [];

    public IReadOnlyCollection<ApprovalItemViewModel> Approvals { get; init; } = [];

    public TransitionFormViewModel Transition { get; init; } = new();

    public CommentFormViewModel NewComment { get; init; } = new();

    /// <summary>Procedure v4.0 sections (readiness, links, communication, validation, PIR, gates).</summary>
    public ReleaseManagement.Application.DTOs.Releases.ReleaseDetailsDto Procedure { get; init; } = null!;

    public string? TechnicalOwnerName { get; init; }

    public string? ReleaseManagerName { get; init; }

    public string? ForecastTitle { get; init; }

    public IReadOnlyCollection<LookupItemViewModel> Users { get; init; } = [];

    public IReadOnlyCollection<FreezeConflictRowViewModel> ActiveFreezes { get; init; } = [];

    public bool IsReleaseManager { get; init; }

    public bool IsTechnicalOwner { get; init; }

    public bool IsProductOwner { get; init; }

    public bool CanEditRecord { get; init; }
}

public sealed class FreezeConflictRowViewModel
{
    public Guid FreezePeriodId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string FreezeType { get; init; } = string.Empty;

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public string Authority { get; init; } = string.Empty;

    public bool HasException { get; init; }
}

public sealed class ReleaseStatusSummaryViewModel
{
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

public sealed class TransitionFormViewModel
{
    public Guid ReleaseId { get; set; }

    public ReleaseStatus TargetStatus { get; set; }

    [MaxLength(4000)]
    public string? Comment { get; set; }

    public DateTime? StabilizationEnd { get; set; }

    [MaxLength(2000)]
    public string? StabilizationNotes { get; set; }
}

public sealed class CommentFormViewModel
{
    public Guid ReleaseId { get; set; }

    public Guid? ParentCommentId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class CommentItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentCommentId { get; init; }

    public string Author { get; init; } = string.Empty;

    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedDate { get; init; }

    public bool IsInternal { get; init; }
}

public sealed class AttachmentItemViewModel
{
    public Guid Id { get; init; }

    public string OriginalFileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public DateTime UploadedDate { get; init; }
}

public sealed class ApprovalItemViewModel
{
    public string ApprovalType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? Comment { get; init; }

    public DateTime RequestedDate { get; init; }

    public DateTime? DecisionDate { get; init; }
}

public sealed class StatusHistoryItemViewModel
{
    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public string? Comment { get; init; }

    public DateTime ChangedDate { get; init; }

    public string? ResponsibleRole { get; init; }
}

public sealed class ApprovalDecisionFormViewModel
{
    public Guid ReleaseId { get; set; }

    public ApprovalType ApprovalType { get; set; }

    public ApprovalStatus Decision { get; set; }

    [MaxLength(4000)]
    public string? Comment { get; set; }
}

public sealed class PendingApprovalsPageViewModel
{
    public IReadOnlyCollection<PendingApprovalRowViewModel> Items { get; init; } = [];
}

public sealed class PendingApprovalRowViewModel
{
    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? ProductName { get; init; }

    public DateTime RequestedDate { get; init; }

    public DateTime PlannedReleaseDate { get; init; }

    public ApprovalType ApprovalType { get; init; }

    public DateTime? SlaDueDate { get; init; }
}

public sealed class ReadinessQueueRowViewModel
{
    public Guid ReleaseId { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? ProductName { get; init; }

    public ReleaseCategory Category { get; init; }

    public ExecutionMode ExecutionMode { get; init; }

    public DateTime PlannedWindowStart { get; init; }

    public string PendingControls { get; init; } = string.Empty;
}

public sealed class PendingWorkPageViewModel
{
    public IReadOnlyCollection<ReadinessQueueRowViewModel> Readiness { get; init; } = [];

    public IReadOnlyCollection<PendingApprovalRowViewModel> Legacy { get; init; } = [];
}

public sealed class CalendarPageViewModel
{
    public IReadOnlyCollection<CalendarRowViewModel> Releases { get; init; } = [];

    public IReadOnlyCollection<FreezeConflictRowViewModel> Freezes { get; init; } = [];
}

public sealed class CalendarRowViewModel
{
    public Guid Id { get; init; }

    public string ReleaseNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Track { get; init; } = string.Empty;

    public ReleaseCategory Category { get; init; }

    public ExecutionMode ExecutionMode { get; init; }

    public string? ProductName { get; init; }

    public string? EnvironmentName { get; init; }

    public string Services { get; init; } = string.Empty;

    public DateTime WindowStart { get; init; }

    public DateTime WindowEnd { get; init; }

    public string? ReleaseManagerName { get; init; }

    public bool DowntimeRequired { get; init; }

    public int? ExpectedDowntimeMinutes { get; init; }

    public bool PlannedMaintenance { get; init; }

    public string KeyDependencies { get; init; } = string.Empty;

    public ReleaseStatus CurrentStatus { get; init; }

    public string CurrentStatusDisplay { get; init; } = string.Empty;

    public bool InFreeze { get; init; }
}

public sealed class ForecastPageViewModel
{
    public int Year { get; init; }

    public int Quarter { get; init; }

    public IReadOnlyCollection<ReleaseManagement.Application.DTOs.Procedure.ReleaseForecastDto> Items { get; init; } = [];

    public IReadOnlyCollection<LookupItemViewModel> Products { get; init; } = [];

    public ForecastFormViewModel Form { get; init; } = new();
}

public sealed class ForecastFormViewModel
{
    public Guid? Id { get; set; }

    public int Year { get; set; } = DateTime.UtcNow.Year;

    public int Quarter { get; set; } = (DateTime.UtcNow.Month - 1) / 3 + 1;

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public ReleaseCategory Category { get; set; } = ReleaseCategory.Normal;

    [Required]
    [MaxLength(200)]
    public string Team { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? ExpectedDate { get; set; }

    [MaxLength(2000)]
    public string? Dependencies { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
}

public sealed class FreezePageViewModel
{
    public IReadOnlyCollection<ReleaseManagement.Application.DTOs.Procedure.FreezePeriodDto> Items { get; init; } = [];

    public FreezeFormViewModel Form { get; init; } = new();
}

public sealed class FreezeFormViewModel
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public FreezeType FreezeType { get; set; } = FreezeType.ItTechnical;

    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    [Required]
    [MaxLength(300)]
    public string Authority { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public sealed class KpiPageViewModel
{
    public IReadOnlyCollection<ReleaseManagement.Application.DTOs.Procedure.MonthlyKpiDto> Months { get; init; } = [];

    public int OpenReviews { get; init; }

    public int ActiveFreezes { get; init; }

    public int ReleasesInReadiness { get; init; }
}
