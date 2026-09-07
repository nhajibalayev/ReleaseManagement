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
