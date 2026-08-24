namespace ReleaseManagement.Domain.Enums;

public enum ProductAccessType
{
    View = 1,
    CreateRelease = 2,
    Manage = 3
}

public enum ApprovalType
{
    ReleaseManager = 1,
    Pentest = 2,
    InfoSec = 3,
    Business = 4
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    ChangesRequired = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum CommentType
{
    General = 1,
    Review = 2,
    Deployment = 3,
    System = 4
}

public enum AttachmentType
{
    General = 1,
    TestEvidence = 2,
    PentestReport = 3,
    SecurityReport = 4,
    DeploymentEvidence = 5
}

public enum DeploymentStatus
{
    Pending = 1,
    InProgress = 2,
    Succeeded = 3,
    Failed = 4,
    RollbackInProgress = 5,
    RolledBack = 6
}

public enum NotificationType
{
    Information = 1,
    ActionRequired = 2,
    Approval = 3,
    Warning = 4,
    Deployment = 5
}

public enum SynchronizationStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}
