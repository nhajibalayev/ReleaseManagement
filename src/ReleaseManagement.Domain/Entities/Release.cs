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
    private readonly List<ReleaseReference> _references = [];
    private readonly List<ReadinessControl> _readinessControls = [];
    private readonly List<ReleaseCommunication> _communications = [];

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
        Track = ReleaseTrack.Application;
        Category = ReleaseCategory.Minor;
        ExecutionMode = ExecutionMode.Planned;
        RecoveryApproach = RecoveryApproach.StandardPipelineRollback;
        PlannedWindowStart = PlannedReleaseDate;
        PlannedWindowEnd = PlannedReleaseDate.AddHours(1);
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

    public IReadOnlyCollection<ReleaseService> Services => _services.AsReadOnly();

    public IReadOnlyCollection<ReleaseWorkItem> WorkItems => _workItems.AsReadOnly();

    public IReadOnlyCollection<ReleaseApproval> Approvals => _approvals.AsReadOnly();

    public IReadOnlyCollection<ReleaseComment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<ReleaseAttachment> Attachments => _attachments.AsReadOnly();

    public IReadOnlyCollection<ReleaseStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public IReadOnlyCollection<DeploymentRecord> DeploymentRecords => _deploymentRecords.AsReadOnly();

    // ---- Procedure v4.0 fields -------------------------------------------------

    /// <summary>§1.2 — Application / Infrastructure (Application only today).</summary>
    public ReleaseTrack Track { get; private set; }

    /// <summary>§4.1 — computed from <see cref="ClassificationCriteria"/>.</summary>
    public ReleaseCategory Category { get; private set; }

    public ClassificationCriteria ClassificationCriteria { get; private set; }

    /// <summary>§4.2 / §6.2.</summary>
    public ExecutionMode ExecutionMode { get; private set; }

    public string? ExpeditedJustification { get; private set; }

    /// <summary>§6.2 — IT Department Director authorization reference (significant impact only).</summary>
    public string? DirectorApprovalReference { get; private set; }

    /// <summary>§5.2 — pentest / assessment triggers.</summary>
    public SecurityTriggers SecurityTriggers { get; private set; }

    /// <summary>§2 — Technical Owner (may be the same person as the Release Manager).</summary>
    public Guid? TechnicalOwnerUserId { get; private set; }

    public Guid? ReleaseManagerUserId { get; private set; }

    /// <summary>§3.2 / §8.2 — planned deployment window.</summary>
    public DateTime PlannedWindowStart { get; private set; }

    public DateTime PlannedWindowEnd { get; private set; }

    public DateTime? ActualWindowStart { get; private set; }

    public DateTime? ActualWindowEnd { get; private set; }

    /// <summary>§6.1 — Planned Maintenance = Yes distinguishes maintenance from release-related incidents.</summary>
    public bool PlannedMaintenance { get; private set; }

    public string? MaintenanceApprovalReference { get; private set; }

    /// <summary>§5 — monitoring / support / capacity are affected (operational readiness required).</summary>
    public bool OperationalImpact { get; private set; }

    public string KeyDependencies { get; private set; } = string.Empty;

    /// <summary>§5.3 — recovery depth by category.</summary>
    public RecoveryApproach RecoveryApproach { get; private set; }

    public string RecoveryDecisionPoints { get; private set; } = string.Empty;

    public string RecoveryResponsibleParties { get; private set; } = string.Empty;

    /// <summary>§7.2 — stabilization / monitoring period after validation.</summary>
    public DateTime? StabilizationStart { get; private set; }

    public DateTime? StabilizationEnd { get; private set; }

    public string? StabilizationNotes { get; private set; }

    /// <summary>§8.2 — final outcome.</summary>
    public ReleaseOutcome? Outcome { get; private set; }

    public string? OutcomeNotes { get; private set; }

    /// <summary>§3.1 — linked quarterly forecast entry.</summary>
    public Guid? ForecastId { get; private set; }

    public IReadOnlyCollection<ReleaseReference> References => _references.AsReadOnly();

    public IReadOnlyCollection<ReadinessControl> ReadinessControls => _readinessControls.AsReadOnly();

    public IReadOnlyCollection<ReleaseCommunication> Communications => _communications.AsReadOnly();

    public bool IsExpedited => ExecutionMode == ExecutionMode.Expedited;

    public bool HasDatabaseChanges => _services.Any(item => item.DatabaseChanges);

    /// <summary>§4.1 / §4.2 — classification and execution mode (Draft / ReturnedForRevision only).</summary>
    public void UpdateClassification(
        ClassificationCriteria criteria,
        SecurityTriggers securityTriggers,
        ExecutionMode executionMode,
        string? expeditedJustification,
        string? directorApprovalReference,
        DateTime updatedDateUtc)
    {
        EnsureEditable();

        if (executionMode == ExecutionMode.Expedited && string.IsNullOrWhiteSpace(expeditedJustification))
        {
            throw new ArgumentException(
                "Expedited execution requires an urgency justification (§6.2).",
                nameof(expeditedJustification));
        }

        ClassificationCriteria = criteria;
        SecurityTriggers = securityTriggers;
        ExecutionMode = executionMode;
        ExpeditedJustification = executionMode == ExecutionMode.Expedited
            ? expeditedJustification!.Trim()
            : null;
        DirectorApprovalReference = string.IsNullOrWhiteSpace(directorApprovalReference)
            ? null
            : directorApprovalReference.Trim();
        RecalculateCategory();
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§3.2 / §6.1 — window, maintenance and dependencies (Draft / ReturnedForRevision only).</summary>
    public void UpdateSchedule(
        DateTime plannedWindowStartUtc,
        DateTime plannedWindowEndUtc,
        bool plannedMaintenance,
        string? maintenanceApprovalReference,
        bool operationalImpact,
        string? keyDependencies,
        DateTime updatedDateUtc)
    {
        EnsureEditable();
        ApplyWindow(plannedWindowStartUtc, plannedWindowEndUtc);
        PlannedMaintenance = plannedMaintenance;
        MaintenanceApprovalReference = string.IsNullOrWhiteSpace(maintenanceApprovalReference)
            ? null
            : maintenanceApprovalReference.Trim();
        OperationalImpact = operationalImpact;
        KeyDependencies = keyDependencies?.Trim() ?? string.Empty;
        RecalculateCategory();
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§3.2 — Release Manager reschedules a planned window after submission.</summary>
    public void Reschedule(DateTime plannedWindowStartUtc, DateTime plannedWindowEndUtc, DateTime updatedDateUtc)
    {
        if (CurrentStatus is
            ReleaseStatus.DeploymentInProgress or
            ReleaseStatus.Deployed or
            ReleaseStatus.Stabilization or
            ReleaseStatus.DeploymentFailed or
            ReleaseStatus.RollbackInProgress ||
            ReleaseWorkflowRules.IsTerminal(CurrentStatus))
        {
            throw new InvalidOperationException(
                $"Release cannot be rescheduled while its status is {CurrentStatus}.");
        }

        ApplyWindow(plannedWindowStartUtc, plannedWindowEndUtc);
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§2 — owners are assigned by the Release Manager; TO may equal RM.</summary>
    public void AssignOwners(Guid? technicalOwnerUserId, Guid? releaseManagerUserId, DateTime updatedDateUtc)
    {
        TechnicalOwnerUserId = technicalOwnerUserId == Guid.Empty ? null : technicalOwnerUserId;
        ReleaseManagerUserId = releaseManagerUserId == Guid.Empty ? null : releaseManagerUserId;
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§5.3 — recovery approach (Draft / ReturnedForRevision only).</summary>
    public void UpdateRecovery(
        RecoveryApproach approach,
        string? decisionPoints,
        string? responsibleParties,
        DateTime updatedDateUtc)
    {
        EnsureEditable();
        RecoveryApproach = approach;
        RecoveryDecisionPoints = decisionPoints?.Trim() ?? string.Empty;
        RecoveryResponsibleParties = responsibleParties?.Trim() ?? string.Empty;
        MarkUpdated(updatedDateUtc);
    }

    public void LinkForecast(Guid? forecastId, DateTime updatedDateUtc)
    {
        ForecastId = forecastId == Guid.Empty ? null : forecastId;
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§7.2 — stabilization period is set when the release enters Stabilization.</summary>
    public void SetStabilization(DateTime startUtc, DateTime endUtc, string? notes, DateTime updatedDateUtc)
    {
        startUtc = EnsureUtc(startUtc, nameof(startUtc));
        endUtc = EnsureUtc(endUtc, nameof(endUtc));

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Stabilization end must be after its start.", nameof(endUtc));
        }

        StabilizationStart = startUtc;
        StabilizationEnd = endUtc;
        StabilizationNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated(updatedDateUtc);
    }

    /// <summary>§8.2 — final outcome recorded before closure or on rollback / cancellation.</summary>
    public void RecordOutcome(ReleaseOutcome outcome, string? notes, DateTime updatedDateUtc)
    {
        Outcome = outcome;
        OutcomeNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated(updatedDateUtc);
    }

    public void AddReference(ReleaseReference reference, DateTime updatedDateUtc)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (reference.ReleaseId != Id)
        {
            throw new ArgumentException("Reference belongs to a different release.", nameof(reference));
        }

        if (_references.Any(item =>
                item.ReferenceType == reference.ReferenceType &&
                string.Equals(item.ExternalId, reference.ExternalId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The reference is already linked to this release.");
        }

        _references.Add(reference);
        MarkUpdated(updatedDateUtc);
    }

    public bool RemoveReference(Guid referenceId, DateTime updatedDateUtc)
    {
        var existing = _references.SingleOrDefault(item => item.Id == referenceId);
        if (existing is null)
        {
            return false;
        }

        _references.Remove(existing);
        MarkUpdated(updatedDateUtc);
        return true;
    }

    public void AddReadinessControl(ReadinessControl control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (control.ReleaseId != Id)
        {
            throw new ArgumentException("Control belongs to a different release.", nameof(control));
        }

        if (_readinessControls.Any(item => item.ControlType == control.ControlType))
        {
            throw new InvalidOperationException("The readiness control already exists for this release.");
        }

        _readinessControls.Add(control);
    }

    public void AddCommunication(ReleaseCommunication communication, DateTime updatedDateUtc)
    {
        ArgumentNullException.ThrowIfNull(communication);

        if (communication.ReleaseId != Id)
        {
            throw new ArgumentException("Communication belongs to a different release.", nameof(communication));
        }

        _communications.Add(communication);
        MarkUpdated(updatedDateUtc);
    }

    private void ApplyWindow(DateTime plannedWindowStartUtc, DateTime plannedWindowEndUtc)
    {
        plannedWindowStartUtc = EnsureUtc(plannedWindowStartUtc, nameof(plannedWindowStartUtc));
        plannedWindowEndUtc = EnsureUtc(plannedWindowEndUtc, nameof(plannedWindowEndUtc));

        if (plannedWindowEndUtc <= plannedWindowStartUtc)
        {
            throw new ArgumentException(
                "Planned window end must be after the window start.",
                nameof(plannedWindowEndUtc));
        }

        PlannedWindowStart = plannedWindowStartUtc;
        PlannedWindowEnd = plannedWindowEndUtc;
        PlannedReleaseDate = plannedWindowStartUtc;
    }

    private void RecalculateCategory()
    {
        Category = ReleaseClassificationRules.Classify(
            ClassificationCriteria,
            HasDatabaseChanges,
            DowntimeRequired);
    }

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
        if (PlannedWindowStart == default || PlannedWindowEnd <= PlannedReleaseDate)
        {
            PlannedWindowStart = PlannedReleaseDate;
            PlannedWindowEnd = PlannedReleaseDate.AddHours(1);
        }

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
        RecalculateCategory();
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
        RecalculateCategory();
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

        if (targetStatus == ReleaseStatus.DeploymentInProgress)
        {
            ActualWindowStart ??= changedDateUtc;
        }

        if (targetStatus == ReleaseStatus.Deployed)
        {
            ActualReleaseDate = changedDateUtc;
            ActualWindowEnd = changedDateUtc;
        }

        if (targetStatus == ReleaseStatus.RolledBack)
        {
            Outcome = ReleaseOutcome.RolledBack;
        }

        if (targetStatus == ReleaseStatus.Cancelled)
        {
            Outcome = ReleaseOutcome.Cancelled;
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
