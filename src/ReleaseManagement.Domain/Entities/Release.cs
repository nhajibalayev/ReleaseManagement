using ReleaseManagement.Domain.Common;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Domain.Entities;

public sealed class Release : AuditableEntity
{
    private readonly List<ReleaseService> _services = [];
    private readonly List<ReleaseWorkItem> _workItems = [];
    private readonly List<ReleaseApproval> _approvals = [];
    private readonly List<ReleaseComment> _comments = [];
    private readonly List<ReleaseAttachment> _attachments = [];
    private readonly List<ReleaseStatusHistory> _statusHistory = [];
    private readonly List<DeploymentRecord> _deploymentRecords = [];

    private Release()
    {
    }

    public Release(
        Guid id,
        string releaseNumber,
        string title,
        string description,
        Guid productId,
        Guid environmentId,
        DateTime plannedReleaseDateUtc,
        Guid createdByUserId,
        DateTime createdDateUtc)
        : base(id, createdDateUtc)
    {
        ReleaseNumber = Required(releaseNumber, nameof(releaseNumber));
        Title = Required(title, nameof(title));
        Description = Required(description, nameof(description));
        ProductId = RequiredId(productId, nameof(productId));
        EnvironmentId = RequiredId(environmentId, nameof(environmentId));
        PlannedReleaseDate = EnsureUtc(plannedReleaseDateUtc, nameof(plannedReleaseDateUtc));
        CreatedByUserId = RequiredId(createdByUserId, nameof(createdByUserId));
        CurrentResponsibleUserId = createdByUserId;
        CurrentResponsibleRole = Constants.RoleNames.ProductOwner;
        CurrentStatus = ReleaseStatus.Draft;
        ReleaseType = ReleaseType.Standard;
        Priority = ReleasePriority.Medium;
        RiskLevel = RiskLevel.Medium;
    }

    public string ReleaseNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid ProductId { get; private set; }

    public ReleaseType ReleaseType { get; private set; }

    public ReleaseStatus CurrentStatus { get; private set; }

    public ReleasePriority Priority { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public DateTime PlannedReleaseDate { get; private set; }

    public DateTime? ActualReleaseDate { get; private set; }

    public string ReleaseVersion { get; private set; } = string.Empty;

    public string BusinessReason { get; private set; } = string.Empty;

    public string ImpactDescription { get; private set; } = string.Empty;

    public RiskLevel RiskLevel { get; private set; }

    public string RiskDescription { get; private set; } = string.Empty;

    public string DeploymentPlan { get; private set; } = string.Empty;

    public string RollbackPlan { get; private set; } = string.Empty;

    public string TestingSummary { get; private set; } = string.Empty;

    public string MonitoringPlan { get; private set; } = string.Empty;

    public string PostReleaseValidationPlan { get; private set; } = string.Empty;

    public bool DowntimeRequired { get; private set; }

    public int? ExpectedDowntimeMinutes { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? CurrentResponsibleUserId { get; private set; }

    public string? CurrentResponsibleRole { get; private set; }

    public DateTime? SubmittedDate { get; private set; }

    public DateTime? ClosedDate { get; private set; }

    public int? AzureDevOpsWorkItemId { get; private set; }

    public string? AzureDevOpsWorkItemUrl { get; private set; }

    /// <summary>
    /// PostgreSQL system column used as optimistic concurrency token.
    /// Public setter required so EF can refresh the value after SaveChanges.
    /// </summary>
    public uint RowVersion { get; set; }

    public IReadOnlyCollection<ReleaseService> Services => _services.AsReadOnly();

    public IReadOnlyCollection<ReleaseWorkItem> WorkItems => _workItems.AsReadOnly();

    public IReadOnlyCollection<ReleaseApproval> Approvals => _approvals.AsReadOnly();

    public IReadOnlyCollection<ReleaseComment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<ReleaseAttachment> Attachments => _attachments.AsReadOnly();

    public IReadOnlyCollection<ReleaseStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public IReadOnlyCollection<DeploymentRecord> DeploymentRecords => _deploymentRecords.AsReadOnly();

    public void UpdateGeneralInformation(
        string title,
        string description,
        ReleaseType releaseType,
        ReleasePriority priority,
        Guid environmentId,
        DateTime plannedReleaseDateUtc,
        string releaseVersion,
        string businessReason,
        string impactDescription,
        DateTime updatedDateUtc)
    {
        EnsureEditable();
        Title = Required(title, nameof(title));
        Description = Required(description, nameof(description));
        ReleaseType = releaseType;
        Priority = priority;
        EnvironmentId = RequiredId(environmentId, nameof(environmentId));
        PlannedReleaseDate = EnsureUtc(plannedReleaseDateUtc, nameof(plannedReleaseDateUtc));
        ReleaseVersion = releaseVersion?.Trim() ?? string.Empty;
        BusinessReason = businessReason?.Trim() ?? string.Empty;
        ImpactDescription = impactDescription?.Trim() ?? string.Empty;
        MarkUpdated(updatedDateUtc);
    }

    public void UpdateReadiness(
        string testingSummary,
        RiskLevel riskLevel,
        string riskDescription,
        string deploymentPlan,
        string rollbackPlan,
        string monitoringPlan,
        string postReleaseValidationPlan,
        bool downtimeRequired,
        int? expectedDowntimeMinutes,
        DateTime updatedDateUtc)
    {
        EnsureEditable();

        if (expectedDowntimeMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedDowntimeMinutes),
                "Expected downtime cannot be negative.");
        }

        if (!downtimeRequired && expectedDowntimeMinutes is not null)
        {
            throw new ArgumentException(
                "Expected downtime must be empty when downtime is not required.",
                nameof(expectedDowntimeMinutes));
        }

        TestingSummary = testingSummary?.Trim() ?? string.Empty;
        RiskLevel = riskLevel;
        RiskDescription = riskDescription?.Trim() ?? string.Empty;
        DeploymentPlan = deploymentPlan?.Trim() ?? string.Empty;
        RollbackPlan = rollbackPlan?.Trim() ?? string.Empty;
        MonitoringPlan = monitoringPlan?.Trim() ?? string.Empty;
        PostReleaseValidationPlan = postReleaseValidationPlan?.Trim() ?? string.Empty;
        DowntimeRequired = downtimeRequired;
        ExpectedDowntimeMinutes = expectedDowntimeMinutes;
        MarkUpdated(updatedDateUtc);
    }

    public void AddService(ReleaseService releaseService, DateTime updatedDateUtc)
    {
        ArgumentNullException.ThrowIfNull(releaseService);
        EnsureEditable();

        if (releaseService.ReleaseId != Id)
        {
            throw new ArgumentException("Service belongs to a different release.", nameof(releaseService));
        }

        if (_services.Any(item => item.ServiceId == releaseService.ServiceId))
        {
            throw new InvalidOperationException("The service is already included in this release.");
        }

        _services.Add(releaseService);
        MarkUpdated(updatedDateUtc);
    }

    public void ClearServices(DateTime updatedDateUtc)
    {
        EnsureEditable();
        _services.Clear();
        MarkUpdated(updatedDateUtc);
    }

    public void AddApproval(ReleaseApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        if (approval.ReleaseId != Id)
        {
            throw new ArgumentException("Approval belongs to a different release.", nameof(approval));
        }

        _approvals.Add(approval);
    }

    public void AddComment(ReleaseComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (comment.ReleaseId != Id)
        {
            throw new ArgumentException("Comment belongs to a different release.", nameof(comment));
        }

        _comments.Add(comment);
    }

    public void AddDeploymentRecord(DeploymentRecord deploymentRecord)
    {
        ArgumentNullException.ThrowIfNull(deploymentRecord);

        if (deploymentRecord.ReleaseId != Id)
        {
            throw new ArgumentException(
                "Deployment record belongs to a different release.",
                nameof(deploymentRecord));
        }

        _deploymentRecords.Add(deploymentRecord);
    }

    public void TransitionTo(
        ReleaseStatus targetStatus,
        IReadOnlyCollection<string> actorRoles,
        Guid changedByUserId,
        DateTime changedDateUtc,
        string? comment,
        Guid? responsibleUserId,
        string? responsibleRole)
    {
        changedByUserId = RequiredId(changedByUserId, nameof(changedByUserId));
        changedDateUtc = EnsureUtc(changedDateUtc, nameof(changedDateUtc));
        ReleaseWorkflowRules.EnsureCanTransition(CurrentStatus, targetStatus, actorRoles, comment);

        var previousStatus = CurrentStatus;
        CurrentStatus = targetStatus;
        CurrentResponsibleUserId = responsibleUserId;
        CurrentResponsibleRole = string.IsNullOrWhiteSpace(responsibleRole)
            ? null
            : responsibleRole.Trim();

        if (targetStatus == ReleaseStatus.Submitted)
        {
            SubmittedDate ??= changedDateUtc;
        }

        if (targetStatus == ReleaseStatus.Deployed)
        {
            ActualReleaseDate = changedDateUtc;
        }

        if (targetStatus == ReleaseStatus.Closed)
        {
            ClosedDate = changedDateUtc;
        }

        _statusHistory.Add(
            new ReleaseStatusHistory(
                Guid.NewGuid(),
                Id,
                previousStatus,
                targetStatus,
                changedByUserId,
                changedDateUtc,
                comment,
                responsibleUserId,
                CurrentResponsibleRole));

        MarkUpdated(changedDateUtc);
    }

    public void SetAzureDevOpsWorkItem(int workItemId, string workItemUrl, DateTime updatedDateUtc)
    {
        if (workItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workItemId));
        }

        AzureDevOpsWorkItemId = workItemId;
        AzureDevOpsWorkItemUrl = Required(workItemUrl, nameof(workItemUrl));
        MarkUpdated(updatedDateUtc);
    }

    private void EnsureEditable()
    {
        if (CurrentStatus is not (ReleaseStatus.Draft or ReleaseStatus.ReturnedForRevision))
        {
            throw new InvalidOperationException(
                $"Release cannot be edited while its status is {CurrentStatus}.");
        }
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return value;
    }
}
