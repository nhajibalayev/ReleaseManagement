using ReleaseManagement.Domain.Common;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Entities;

public sealed class DeploymentRecord : Entity
{
    private DeploymentRecord()
    {
    }

    public DeploymentRecord(
        Guid id,
        Guid releaseId,
        Guid environmentId,
        Guid startedByUserId,
        DateTime startedDateUtc,
        string? pipelineUrl,
        string? pipelineRunId,
        string? buildNumber)
        : base(id)
    {
        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        EnvironmentId = RequiredId(environmentId, nameof(environmentId));
        StartedByUserId = RequiredId(startedByUserId, nameof(startedByUserId));
        StartedDate = EnsureUtc(startedDateUtc, nameof(startedDateUtc));
        PipelineUrl = pipelineUrl?.Trim();
        PipelineRunId = pipelineRunId?.Trim();
        BuildNumber = buildNumber?.Trim();
        Status = DeploymentStatus.InProgress;
    }

    public Guid ReleaseId { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public DeploymentStatus Status { get; private set; }

    public Guid StartedByUserId { get; private set; }

    public DateTime StartedDate { get; private set; }

    public Guid? CompletedByUserId { get; private set; }

    public DateTime? CompletedDate { get; private set; }

    public string? PipelineUrl { get; private set; }

    public string? PipelineRunId { get; private set; }

    public string? BuildNumber { get; private set; }

    public string? Comment { get; private set; }

    public string? ErrorDetails { get; private set; }

    public bool RollbackRequired { get; private set; }

    public string? RollbackResult { get; private set; }

    public void Complete(Guid completedByUserId, DateTime completedDateUtc, string? comment)
    {
        EnsureInProgress();
        Status = DeploymentStatus.Succeeded;
        CompletedByUserId = RequiredId(completedByUserId, nameof(completedByUserId));
        CompletedDate = EnsureUtc(completedDateUtc, nameof(completedDateUtc));
        Comment = comment?.Trim();
    }

    public void Fail(
        Guid completedByUserId,
        DateTime completedDateUtc,
        string errorDetails,
        bool rollbackRequired)
    {
        EnsureInProgress();

        if (string.IsNullOrWhiteSpace(errorDetails))
        {
            throw new ArgumentException("Error details are required.", nameof(errorDetails));
        }

        Status = DeploymentStatus.Failed;
        CompletedByUserId = RequiredId(completedByUserId, nameof(completedByUserId));
        CompletedDate = EnsureUtc(completedDateUtc, nameof(completedDateUtc));
        ErrorDetails = errorDetails.Trim();
        RollbackRequired = rollbackRequired;
    }

    public void StartRollback()
    {
        if (Status != DeploymentStatus.Failed || !RollbackRequired)
        {
            throw new InvalidOperationException("Rollback cannot be started for this deployment.");
        }

        Status = DeploymentStatus.RollbackInProgress;
    }

    public void CompleteRollback(string rollbackResult)
    {
        if (Status != DeploymentStatus.RollbackInProgress)
        {
            throw new InvalidOperationException("Rollback is not in progress.");
        }

        if (string.IsNullOrWhiteSpace(rollbackResult))
        {
            throw new ArgumentException("Rollback result is required.", nameof(rollbackResult));
        }

        Status = DeploymentStatus.RolledBack;
        RollbackResult = rollbackResult.Trim();
    }

    private void EnsureInProgress()
    {
        if (Status != DeploymentStatus.InProgress)
        {
            throw new InvalidOperationException("Deployment is not in progress.");
        }
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
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

public sealed class AzureDevOpsMapping : Entity
{
    private AzureDevOpsMapping()
    {
    }

    public AzureDevOpsMapping(
        Guid id,
        Guid releaseId,
        string organizationUrl,
        string projectName,
        int workItemId,
        string workItemUrl)
        : base(id)
    {
        if (releaseId == Guid.Empty)
        {
            throw new ArgumentException("Release identifier cannot be empty.", nameof(releaseId));
        }

        if (workItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workItemId));
        }

        ReleaseId = releaseId;
        OrganizationUrl = Required(organizationUrl, nameof(organizationUrl));
        ProjectName = Required(projectName, nameof(projectName));
        WorkItemId = workItemId;
        WorkItemUrl = Required(workItemUrl, nameof(workItemUrl));
        LastSynchronizationStatus = SynchronizationStatus.Pending;
    }

    public Guid ReleaseId { get; private set; }

    public string OrganizationUrl { get; private set; } = string.Empty;

    public string ProjectName { get; private set; } = string.Empty;

    public int WorkItemId { get; private set; }

    public string WorkItemUrl { get; private set; } = string.Empty;

    public DateTime? LastSynchronizedDate { get; private set; }

    public SynchronizationStatus LastSynchronizationStatus { get; private set; }

    public string? LastSynchronizationError { get; private set; }

    public void RecordSynchronizationSuccess(DateTime synchronizedDateUtc)
    {
        LastSynchronizedDate = EnsureUtc(synchronizedDateUtc, nameof(synchronizedDateUtc));
        LastSynchronizationStatus = SynchronizationStatus.Succeeded;
        LastSynchronizationError = null;
    }

    public void RecordSynchronizationFailure(DateTime synchronizedDateUtc, string error)
    {
        LastSynchronizedDate = EnsureUtc(synchronizedDateUtc, nameof(synchronizedDateUtc));
        LastSynchronizationStatus = SynchronizationStatus.Failed;
        LastSynchronizationError = Required(error, nameof(error));
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
    }
}

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    public Notification(
        Guid id,
        Guid userId,
        Guid? releaseId,
        string title,
        string message,
        NotificationType notificationType,
        DateTime createdDateUtc)
        : base(id)
    {
        UserId = RequiredId(userId, nameof(userId));
        ReleaseId = releaseId;
        Title = Required(title, nameof(title));
        Message = Required(message, nameof(message));
        NotificationType = notificationType;
        CreatedDate = EnsureUtc(createdDateUtc, nameof(createdDateUtc));
    }

    public Guid UserId { get; private set; }

    public Guid? ReleaseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public NotificationType NotificationType { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime CreatedDate { get; private set; }

    public DateTime? ReadDate { get; private set; }

    public bool EmailSent { get; private set; }

    public DateTime? EmailSentDate { get; private set; }

    public void MarkAsRead(DateTime readDateUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadDate = EnsureUtc(readDateUtc, nameof(readDateUtc));
    }

    public void MarkEmailSent(DateTime sentDateUtc)
    {
        EmailSent = true;
        EmailSentDate = EnsureUtc(sentDateUtc, nameof(sentDateUtc));
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
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

public sealed class AuditLog : Entity
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        string entityId,
        string? oldValuesJson,
        string? newValuesJson,
        string? ipAddress,
        string? userAgent,
        DateTime createdDateUtc,
        string correlationId)
        : base(id)
    {
        UserId = userId;
        Action = Required(action, nameof(action));
        EntityName = Required(entityName, nameof(entityName));
        EntityId = Required(entityId, nameof(entityId));
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        CreatedDate = EnsureUtc(createdDateUtc, nameof(createdDateUtc));
        CorrelationId = Required(correlationId, nameof(correlationId));
    }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? OldValuesJson { get; private set; }

    public string? NewValuesJson { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CreatedDate { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Date must use UTC.", parameterName);
        }

        return value;
    }
}
