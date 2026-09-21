namespace ReleaseManagement.Domain.Enums;

public enum ReleaseStatus
{
    Draft = 1,
    Submitted = 2,
    ReleaseManagerReview = 3,
    ReturnedForRevision = 4,
    PentestReview = 5,
    PentestChangesRequired = 6,
    InfoSecReview = 7,
    InfoSecChangesRequired = 8,
    BusinessApproval = 9,
    BusinessChangesRequired = 10,
    Approved = 11,
    ReadyForRelease = 12,
    DeploymentInProgress = 13,
    Deployed = 14,
    DeploymentFailed = 15,
    RollbackInProgress = 16,
    RolledBack = 17,
    Rejected = 18,
    Closed = 19,
    Cancelled = 20,
    QaReview = 21,
    QaChangesRequired = 22,
    RiskReview = 23,
    RiskChangesRequired = 24,
    ChapterLeadReview = 25,
    ChapterLeadChangesRequired = 26,

    // Procedure v4.0 alignment: readiness-checklist model (§5) and post-release stabilization (§7.2).
    ReadinessInProgress = 27,
    Stabilization = 28
}

public enum ReleaseType
{
    Standard = 1,
    Emergency = 2,
    Hotfix = 3
}

public enum ReleasePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
