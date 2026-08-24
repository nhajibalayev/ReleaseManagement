using ReleaseManagement.Domain.Common;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Entities;

public sealed class ReleaseService : Entity
{
    private ReleaseService()
    {
    }

    public ReleaseService(Guid id, Guid releaseId, Guid serviceId)
        : base(id)
    {
        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        ServiceId = RequiredId(serviceId, nameof(serviceId));
    }

    public Guid ReleaseId { get; private set; }

    public Guid ServiceId { get; private set; }

    public string? BranchName { get; private set; }

    public string? CommitId { get; private set; }

    public string? Version { get; private set; }

    public string? BuildNumber { get; private set; }

    public string? ArtifactUrl { get; private set; }

    public string? RepositoryUrl { get; private set; }

    public bool DatabaseChanges { get; private set; }

    public bool ConfigurationChanges { get; private set; }

    public string? Notes { get; private set; }

    public void SetBuildInformation(
        string? branchName,
        string? commitId,
        string? version,
        string? buildNumber,
        string? artifactUrl,
        string? repositoryUrl)
    {
        BranchName = branchName?.Trim();
        CommitId = commitId?.Trim();
        Version = version?.Trim();
        BuildNumber = buildNumber?.Trim();
        ArtifactUrl = artifactUrl?.Trim();
        RepositoryUrl = repositoryUrl?.Trim();
    }

    public void SetChangeFlags(bool databaseChanges, bool configurationChanges, string? notes)
    {
        DatabaseChanges = databaseChanges;
        ConfigurationChanges = configurationChanges;
        Notes = notes?.Trim();
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

public sealed class ReleaseWorkItem : Entity
{
    private ReleaseWorkItem()
    {
    }

    public ReleaseWorkItem(
        Guid id,
        Guid releaseId,
        int workItemId,
        string workItemType,
        string title,
        string workItemUrl)
        : base(id)
    {
        if (workItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workItemId));
        }

        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        WorkItemId = workItemId;
        WorkItemType = Required(workItemType, nameof(workItemType));
        Title = Required(title, nameof(title));
        WorkItemUrl = Required(workItemUrl, nameof(workItemUrl));
    }

    public Guid ReleaseId { get; private set; }

    public int WorkItemId { get; private set; }

    public string WorkItemType { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string WorkItemUrl { get; private set; } = string.Empty;

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

public sealed class ReleaseApproval : Entity
{
    private ReleaseApproval()
    {
    }

    public ReleaseApproval(
        Guid id,
        Guid releaseId,
        ApprovalType approvalType,
        int sequence,
        Guid? assignedUserId,
        string? assignedRole,
        bool isRequired,
        DateTime requestedDateUtc)
        : base(id)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        if (assignedUserId is null && string.IsNullOrWhiteSpace(assignedRole))
        {
            throw new ArgumentException("Approval must be assigned to a user or role.");
        }

        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        ApprovalType = approvalType;
        Sequence = sequence;
        AssignedUserId = assignedUserId;
        AssignedRole = assignedRole?.Trim();
        Status = ApprovalStatus.Pending;
        IsRequired = isRequired;
        RequestedDate = EnsureUtc(requestedDateUtc, nameof(requestedDateUtc));
    }

    public Guid ReleaseId { get; private set; }

    public ApprovalType ApprovalType { get; private set; }

    public int Sequence { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public string? AssignedRole { get; private set; }

    public ApprovalStatus Status { get; private set; }

    public Guid? DecisionByUserId { get; private set; }

    public DateTime RequestedDate { get; private set; }

    public DateTime? DecisionDate { get; private set; }

    public string? Comment { get; private set; }

    public bool IsRequired { get; private set; }

    public void Decide(
        ApprovalStatus decision,
        Guid decidedByUserId,
        DateTime decisionDateUtc,
        string? comment)
    {
        if (Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Approval has already been decided.");
        }

        if (decision is not (
            ApprovalStatus.Approved or
            ApprovalStatus.ChangesRequired or
            ApprovalStatus.Rejected))
        {
            throw new ArgumentOutOfRangeException(nameof(decision));
        }

        if (decision != ApprovalStatus.Approved && string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException(
                "A comment is required when approval is not granted.",
                nameof(comment));
        }

        Status = decision;
        DecisionByUserId = RequiredId(decidedByUserId, nameof(decidedByUserId));
        DecisionDate = EnsureUtc(decisionDateUtc, nameof(decisionDateUtc));
        Comment = comment?.Trim();
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

public sealed class ReleaseComment : AuditableEntity
{
    private ReleaseComment()
    {
    }

    public ReleaseComment(
        Guid id,
        Guid releaseId,
        Guid userId,
        string comment,
        CommentType commentType,
        bool isInternal,
        Guid? parentCommentId,
        DateTime createdDateUtc)
        : base(id, createdDateUtc)
    {
        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        UserId = RequiredId(userId, nameof(userId));
        Comment = Required(comment, nameof(comment));
        CommentType = commentType;
        IsInternal = isInternal;
        ParentCommentId = parentCommentId;
    }

    public Guid ReleaseId { get; private set; }

    public Guid UserId { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public CommentType CommentType { get; private set; }

    public bool IsInternal { get; private set; }

    public Guid? ParentCommentId { get; private set; }

    public void Edit(string comment, DateTime updatedDateUtc)
    {
        Comment = Required(comment, nameof(comment));
        MarkUpdated(updatedDateUtc);
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

public sealed class ReleaseAttachment : Entity
{
    private ReleaseAttachment()
    {
    }

    public ReleaseAttachment(
        Guid id,
        Guid releaseId,
        string fileName,
        string originalFileName,
        string contentType,
        long fileSize,
        string storagePath,
        Guid uploadedByUserId,
        DateTime uploadedDateUtc,
        AttachmentType attachmentType)
        : base(id)
    {
        if (fileSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize));
        }

        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        FileName = Required(fileName, nameof(fileName));
        OriginalFileName = Required(originalFileName, nameof(originalFileName));
        ContentType = Required(contentType, nameof(contentType));
        FileSize = fileSize;
        StoragePath = Required(storagePath, nameof(storagePath));
        UploadedByUserId = RequiredId(uploadedByUserId, nameof(uploadedByUserId));
        UploadedDate = EnsureUtc(uploadedDateUtc, nameof(uploadedDateUtc));
        AttachmentType = attachmentType;
    }

    public Guid ReleaseId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    public string StoragePath { get; private set; } = string.Empty;

    public Guid UploadedByUserId { get; private set; }

    public DateTime UploadedDate { get; private set; }

    public AttachmentType AttachmentType { get; private set; }

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

public sealed class ReleaseStatusHistory : Entity
{
    private ReleaseStatusHistory()
    {
    }

    public ReleaseStatusHistory(
        Guid id,
        Guid releaseId,
        ReleaseStatus fromStatus,
        ReleaseStatus toStatus,
        Guid changedByUserId,
        DateTime changedDateUtc,
        string? comment,
        Guid? responsibleUserId,
        string? responsibleRole)
        : base(id)
    {
        ReleaseId = RequiredId(releaseId, nameof(releaseId));
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = RequiredId(changedByUserId, nameof(changedByUserId));
        ChangedDate = EnsureUtc(changedDateUtc, nameof(changedDateUtc));
        Comment = comment?.Trim();
        ResponsibleUserId = responsibleUserId;
        ResponsibleRole = responsibleRole?.Trim();
    }

    public Guid ReleaseId { get; private set; }

    public ReleaseStatus FromStatus { get; private set; }

    public ReleaseStatus ToStatus { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public DateTime ChangedDate { get; private set; }

    public string? Comment { get; private set; }

    public Guid? ResponsibleUserId { get; private set; }

    public string? ResponsibleRole { get; private set; }

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
