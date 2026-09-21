using ReleaseManagement.Domain.Common;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Entities;

/// <summary>
/// Procedure v4.0 §1.3 / §8.1 — a link to an authoritative source system
/// (Azure Boards WI / PR / pipeline / artifact, Service Desk Change / Incident, ...).
/// The Release Record references evidence; it does not copy it.
/// </summary>
public sealed class ReleaseReference : Entity
{
    private ReleaseReference()
    {
    }

    public ReleaseReference(
        Guid id,
        Guid releaseId,
        ReleaseReferenceType referenceType,
        string externalId,
        string? url,
        string? title,
        Guid addedByUserId,
        DateTime addedDateUtc)
        : base(id)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        ReferenceType = referenceType;
        ExternalId = Guards.Required(externalId, nameof(externalId));
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        AddedByUserId = Guards.RequiredId(addedByUserId, nameof(addedByUserId));
        AddedDate = Guards.EnsureUtc(addedDateUtc, nameof(addedDateUtc));
    }

    public Guid ReleaseId { get; private set; }

    public ReleaseReferenceType ReferenceType { get; private set; }

    public string ExternalId { get; private set; } = string.Empty;

    public string? Url { get; private set; }

    public string? Title { get; private set; }

    public Guid AddedByUserId { get; private set; }

    public DateTime AddedDate { get; private set; }

    public bool IsSourceReference =>
        ReferenceType is
            ReleaseReferenceType.WorkItem or
            ReleaseReferenceType.PullRequest or
            ReleaseReferenceType.Pipeline or
            ReleaseReferenceType.Artifact or
            ReleaseReferenceType.ServiceDeskChange;
}

/// <summary>
/// Procedure v4.0 §5 — one row of the readiness control table for a release.
/// Status is set by the evidence owner (role); the Release Manager only checks it exists.
/// </summary>
public sealed class ReadinessControl : Entity
{
    private ReadinessControl()
    {
    }

    public ReadinessControl(
        Guid id,
        Guid releaseId,
        ReadinessControlType controlType,
        string ownerRole,
        bool isRequired,
        DateTime createdDateUtc)
        : base(id)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        ControlType = controlType;
        OwnerRole = Guards.Required(ownerRole, nameof(ownerRole));
        IsRequired = isRequired;
        Status = ReadinessControlStatus.Pending;
        UpdatedDate = Guards.EnsureUtc(createdDateUtc, nameof(createdDateUtc));
    }

    public Guid ReleaseId { get; private set; }

    public ReadinessControlType ControlType { get; private set; }

    public string OwnerRole { get; private set; } = string.Empty;

    public bool IsRequired { get; private set; }

    public ReadinessControlStatus Status { get; private set; }

    /// <summary>Link / ID of the evidence in the authoritative source system.</summary>
    public string? EvidenceReference { get; private set; }

    /// <summary>Required for NotRequired, ReadyWithApprovedException and Blocked.</summary>
    public string? Justification { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public DateTime UpdatedDate { get; private set; }

    public bool IsClosed =>
        !IsRequired ||
        Status is
            ReadinessControlStatus.Ready or
            ReadinessControlStatus.ReadyWithApprovedException or
            ReadinessControlStatus.NotRequired;

    public void SetRequired(bool isRequired, DateTime updatedDateUtc)
    {
        IsRequired = isRequired;
        UpdatedDate = Guards.EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }

    public void SetStatus(
        ReadinessControlStatus status,
        string? evidenceReference,
        string? justification,
        Guid updatedByUserId,
        DateTime updatedDateUtc)
    {
        if (status is
                ReadinessControlStatus.NotRequired or
                ReadinessControlStatus.ReadyWithApprovedException or
                ReadinessControlStatus.Blocked &&
            string.IsNullOrWhiteSpace(justification))
        {
            throw new ArgumentException(
                $"A justification is required when the control status is {status}.",
                nameof(justification));
        }

        if (status is ReadinessControlStatus.Ready or ReadinessControlStatus.ReadyWithApprovedException &&
            string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "An evidence reference (link or ID in the source system) is required for Ready.",
                nameof(evidenceReference));
        }

        Status = status;
        EvidenceReference = string.IsNullOrWhiteSpace(evidenceReference) ? null : evidenceReference.Trim();
        Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim();
        UpdatedByUserId = Guards.RequiredId(updatedByUserId, nameof(updatedByUserId));
        UpdatedDate = Guards.EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }

    public void Reset(DateTime updatedDateUtc)
    {
        Status = ReadinessControlStatus.Pending;
        UpdatedDate = Guards.EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }
}

/// <summary>Procedure v4.0 §6.4 — log of impact-based stakeholder communication.</summary>
public sealed class ReleaseCommunication : Entity
{
    private ReleaseCommunication()
    {
    }

    public ReleaseCommunication(
        Guid id,
        Guid releaseId,
        CommunicationType communicationType,
        string audience,
        string channel,
        string message,
        Guid sentByUserId,
        DateTime sentDateUtc)
        : base(id)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        CommunicationType = communicationType;
        Audience = Guards.Required(audience, nameof(audience));
        Channel = Guards.Required(channel, nameof(channel));
        Message = Guards.Required(message, nameof(message));
        SentByUserId = Guards.RequiredId(sentByUserId, nameof(sentByUserId));
        SentDate = Guards.EnsureUtc(sentDateUtc, nameof(sentDateUtc));
    }

    public Guid ReleaseId { get; private set; }

    public CommunicationType CommunicationType { get; private set; }

    public string Audience { get; private set; } = string.Empty;

    public string Channel { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public Guid SentByUserId { get; private set; }

    public DateTime SentDate { get; private set; }
}

/// <summary>Procedure v4.0 §7.2 — mandatory post-release validation (technical + optional business).</summary>
public sealed class PostReleaseValidation : Entity
{
    private PostReleaseValidation()
    {
    }

    public PostReleaseValidation(Guid id, Guid releaseId, bool businessValidationRequired, DateTime createdDateUtc)
        : base(id)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        BusinessValidationRequired = businessValidationRequired;
        TechnicalResult = ValidationResult.Pending;
        BusinessResult = businessValidationRequired ? ValidationResult.Pending : ValidationResult.Passed;
        CreatedDate = Guards.EnsureUtc(createdDateUtc, nameof(createdDateUtc));
    }

    public Guid ReleaseId { get; private set; }

    public DateTime CreatedDate { get; private set; }

    public ValidationResult TechnicalResult { get; private set; }

    public bool HealthCheckPassed { get; private set; }

    public bool SmokeTestPassed { get; private set; }

    public bool MonitoringClean { get; private set; }

    public bool RecoveryNeeded { get; private set; }

    public string? TechnicalNotes { get; private set; }

    public string? TechnicalEvidenceReference { get; private set; }

    public Guid? TechnicalValidatedByUserId { get; private set; }

    public DateTime? TechnicalValidatedDate { get; private set; }

    public bool BusinessValidationRequired { get; private set; }

    public ValidationResult BusinessResult { get; private set; }

    public string? BusinessNotes { get; private set; }

    public Guid? BusinessValidatedByUserId { get; private set; }

    public DateTime? BusinessValidatedDate { get; private set; }

    public bool IsComplete =>
        TechnicalResult != ValidationResult.Pending &&
        (!BusinessValidationRequired || BusinessResult != ValidationResult.Pending);

    public bool IsPassed =>
        IsComplete &&
        TechnicalResult is ValidationResult.Passed or ValidationResult.PassedWithIssues &&
        (!BusinessValidationRequired ||
         BusinessResult is ValidationResult.Passed or ValidationResult.PassedWithIssues);

    public void RecordTechnical(
        ValidationResult result,
        bool healthCheckPassed,
        bool smokeTestPassed,
        bool monitoringClean,
        bool recoveryNeeded,
        string? notes,
        string? evidenceReference,
        Guid validatedByUserId,
        DateTime validatedDateUtc)
    {
        if (result == ValidationResult.Pending)
        {
            throw new ArgumentException("A validation result is required.", nameof(result));
        }

        TechnicalResult = result;
        HealthCheckPassed = healthCheckPassed;
        SmokeTestPassed = smokeTestPassed;
        MonitoringClean = monitoringClean;
        RecoveryNeeded = recoveryNeeded;
        TechnicalNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        TechnicalEvidenceReference = string.IsNullOrWhiteSpace(evidenceReference) ? null : evidenceReference.Trim();
        TechnicalValidatedByUserId = Guards.RequiredId(validatedByUserId, nameof(validatedByUserId));
        TechnicalValidatedDate = Guards.EnsureUtc(validatedDateUtc, nameof(validatedDateUtc));
    }

    public void RecordBusiness(
        ValidationResult result,
        string? notes,
        Guid validatedByUserId,
        DateTime validatedDateUtc)
    {
        if (result == ValidationResult.Pending)
        {
            throw new ArgumentException("A validation result is required.", nameof(result));
        }

        BusinessValidationRequired = true;
        BusinessResult = result;
        BusinessNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        BusinessValidatedByUserId = Guards.RequiredId(validatedByUserId, nameof(validatedByUserId));
        BusinessValidatedDate = Guards.EnsureUtc(validatedDateUtc, nameof(validatedDateUtc));
    }
}

/// <summary>Procedure v4.0 §7.3 — Post-Implementation Review (only when triggered).</summary>
public sealed class PostImplementationReview : Entity
{
    private readonly List<PirAction> _actions = [];

    private PostImplementationReview()
    {
    }

    public PostImplementationReview(
        Guid id,
        Guid releaseId,
        PirTriggers triggers,
        Guid createdByUserId,
        DateTime createdDateUtc)
        : base(id)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        Triggers = triggers;
        Status = PirStatus.Open;
        Format = PirFormat.AsyncEvidenceReview;
        CreatedByUserId = Guards.RequiredId(createdByUserId, nameof(createdByUserId));
        CreatedDate = Guards.EnsureUtc(createdDateUtc, nameof(createdDateUtc));
        UpdatedDate = CreatedDate;
    }

    public Guid ReleaseId { get; private set; }

    public PirTriggers Triggers { get; private set; }

    public PirStatus Status { get; private set; }

    public PirFormat Format { get; private set; }

    public string? RootCause { get; private set; }

    public string? LessonsLearned { get; private set; }

    public string? BacklogReference { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedDate { get; private set; }

    public DateTime UpdatedDate { get; private set; }

    public Guid? CompletedByUserId { get; private set; }

    public DateTime? CompletedDate { get; private set; }

    public IReadOnlyCollection<PirAction> Actions => _actions.AsReadOnly();

    public void AddTriggers(PirTriggers triggers, DateTime updatedDateUtc)
    {
        Triggers |= triggers;
        UpdatedDate = Guards.EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }

    public void Update(
        PirFormat format,
        string? rootCause,
        string? lessonsLearned,
        string? backlogReference,
        DateTime updatedDateUtc)
    {
        EnsureOpen();
        Format = format;
        RootCause = string.IsNullOrWhiteSpace(rootCause) ? null : rootCause.Trim();
        LessonsLearned = string.IsNullOrWhiteSpace(lessonsLearned) ? null : lessonsLearned.Trim();
        BacklogReference = string.IsNullOrWhiteSpace(backlogReference) ? null : backlogReference.Trim();
        Status = PirStatus.InProgress;
        UpdatedDate = Guards.EnsureUtc(updatedDateUtc, nameof(updatedDateUtc));
    }

    public void AddAction(PirAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureOpen();

        if (action.PostImplementationReviewId != Id)
        {
            throw new ArgumentException("Action belongs to a different review.", nameof(action));
        }

        _actions.Add(action);
    }

    public void Complete(Guid completedByUserId, DateTime completedDateUtc)
    {
        EnsureOpen();

        if (string.IsNullOrWhiteSpace(RootCause) || string.IsNullOrWhiteSpace(LessonsLearned))
        {
            throw new InvalidOperationException(
                "Root cause and lessons learned are required before the review can be completed.");
        }

        if (_actions.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one corrective action with an owner and target date is required.");
        }

        Status = PirStatus.Completed;
        CompletedByUserId = Guards.RequiredId(completedByUserId, nameof(completedByUserId));
        CompletedDate = Guards.EnsureUtc(completedDateUtc, nameof(completedDateUtc));
        UpdatedDate = CompletedDate.Value;
    }

    private void EnsureOpen()
    {
        if (Status == PirStatus.Completed)
        {
            throw new InvalidOperationException("The review has already been completed.");
        }
    }
}

public sealed class PirAction : Entity
{
    private PirAction()
    {
    }

    public PirAction(
        Guid id,
        Guid postImplementationReviewId,
        string description,
        string ownerName,
        DateTime targetDateUtc,
        string? reference)
        : base(id)
    {
        PostImplementationReviewId = Guards.RequiredId(postImplementationReviewId, nameof(postImplementationReviewId));
        Description = Guards.Required(description, nameof(description));
        OwnerName = Guards.Required(ownerName, nameof(ownerName));
        TargetDate = Guards.EnsureUtc(targetDateUtc, nameof(targetDateUtc));
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    public Guid PostImplementationReviewId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string OwnerName { get; private set; } = string.Empty;

    public DateTime TargetDate { get; private set; }

    public string? Reference { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedDate { get; private set; }

    public void MarkCompleted(DateTime completedDateUtc)
    {
        IsCompleted = true;
        CompletedDate = Guards.EnsureUtc(completedDateUtc, nameof(completedDateUtc));
    }
}

/// <summary>Procedure v4.0 §3.1 — quarterly release forecast entry (a plan, not an approval).</summary>
public sealed class ReleaseForecast : AuditableEntity
{
    private ReleaseForecast()
    {
    }

    public ReleaseForecast(
        Guid id,
        int year,
        int quarter,
        Guid productId,
        string title,
        ReleaseCategory category,
        string team,
        DateTime? expectedDateUtc,
        string? dependencies,
        string? notes,
        Guid createdByUserId,
        DateTime createdDateUtc)
        : base(id, createdDateUtc)
    {
        if (quarter is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(quarter), "Quarter must be 1..4.");
        }

        Year = year;
        Quarter = quarter;
        ProductId = Guards.RequiredId(productId, nameof(productId));
        Title = Guards.Required(title, nameof(title));
        Category = category;
        Team = Guards.Required(team, nameof(team));
        ExpectedDate = expectedDateUtc;
        Dependencies = string.IsNullOrWhiteSpace(dependencies) ? null : dependencies.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = ForecastStatus.Planned;
        CreatedByUserId = Guards.RequiredId(createdByUserId, nameof(createdByUserId));
    }

    public int Year { get; private set; }

    public int Quarter { get; private set; }

    public Guid ProductId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public ReleaseCategory Category { get; private set; }

    public string Team { get; private set; } = string.Empty;

    public DateTime? ExpectedDate { get; private set; }

    public string? Dependencies { get; private set; }

    public string? Notes { get; private set; }

    public ForecastStatus Status { get; private set; }

    public Guid? ReleaseId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public void Update(
        string title,
        ReleaseCategory category,
        string team,
        DateTime? expectedDateUtc,
        string? dependencies,
        string? notes,
        ForecastStatus status,
        DateTime updatedDateUtc)
    {
        Title = Guards.Required(title, nameof(title));
        Category = category;
        Team = Guards.Required(team, nameof(team));
        ExpectedDate = expectedDateUtc;
        Dependencies = string.IsNullOrWhiteSpace(dependencies) ? null : dependencies.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = status;
        MarkUpdated(updatedDateUtc);
    }

    public void LinkRelease(Guid releaseId, DateTime updatedDateUtc)
    {
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        MarkUpdated(updatedDateUtc);
    }

    public void MarkDelivered(DateTime updatedDateUtc)
    {
        Status = ForecastStatus.Delivered;
        MarkUpdated(updatedDateUtc);
    }
}

/// <summary>Procedure v4.0 §6.3 — Business/Enterprise or IT Technical freeze period managed on the calendar.</summary>
public sealed class FreezePeriod : Entity
{
    private readonly List<FreezeException> _exceptions = [];

    private FreezePeriod()
    {
    }

    public FreezePeriod(
        Guid id,
        string name,
        FreezeType freezeType,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string authority,
        string? description,
        Guid createdByUserId,
        DateTime createdDateUtc)
        : base(id)
    {
        Name = Guards.Required(name, nameof(name));
        FreezeType = freezeType;
        StartDate = Guards.EnsureUtc(startDateUtc, nameof(startDateUtc));
        EndDate = Guards.EnsureUtc(endDateUtc, nameof(endDateUtc));

        if (EndDate <= StartDate)
        {
            throw new ArgumentException("Freeze end must be after the start.", nameof(endDateUtc));
        }

        Authority = Guards.Required(authority, nameof(authority));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true;
        CreatedByUserId = Guards.RequiredId(createdByUserId, nameof(createdByUserId));
        CreatedDate = Guards.EnsureUtc(createdDateUtc, nameof(createdDateUtc));
    }

    public string Name { get; private set; } = string.Empty;

    public FreezeType FreezeType { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    /// <summary>Board / delegated authority (business) or IT Director / BCP-DR plan (technical).</summary>
    public string Authority { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedDate { get; private set; }

    public IReadOnlyCollection<FreezeException> Exceptions => _exceptions.AsReadOnly();

    public bool Overlaps(DateTime windowStartUtc, DateTime windowEndUtc) =>
        IsActive && windowStartUtc < EndDate && windowEndUtc > StartDate;

    public bool HasExceptionFor(Guid releaseId) =>
        _exceptions.Any(item => item.ReleaseId == releaseId);

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AddException(FreezeException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.FreezePeriodId != Id)
        {
            throw new ArgumentException("Exception belongs to a different freeze period.", nameof(exception));
        }

        if (HasExceptionFor(exception.ReleaseId))
        {
            throw new InvalidOperationException("An exception already exists for this release.");
        }

        _exceptions.Add(exception);
    }
}

public sealed class FreezeException : Entity
{
    private FreezeException()
    {
    }

    public FreezeException(
        Guid id,
        Guid freezePeriodId,
        Guid releaseId,
        string approvedBy,
        string justification,
        Guid recordedByUserId,
        DateTime approvedDateUtc)
        : base(id)
    {
        FreezePeriodId = Guards.RequiredId(freezePeriodId, nameof(freezePeriodId));
        ReleaseId = Guards.RequiredId(releaseId, nameof(releaseId));
        ApprovedBy = Guards.Required(approvedBy, nameof(approvedBy));
        Justification = Guards.Required(justification, nameof(justification));
        RecordedByUserId = Guards.RequiredId(recordedByUserId, nameof(recordedByUserId));
        ApprovedDate = Guards.EnsureUtc(approvedDateUtc, nameof(approvedDateUtc));
    }

    public Guid FreezePeriodId { get; private set; }

    public Guid ReleaseId { get; private set; }

    /// <summary>Freeze authority that granted the exception.</summary>
    public string ApprovedBy { get; private set; } = string.Empty;

    public string Justification { get; private set; } = string.Empty;

    public Guid RecordedByUserId { get; private set; }

    public DateTime ApprovedDate { get; private set; }
}

internal static class Guards
{
    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return value;
    }

    public static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
    }
}
