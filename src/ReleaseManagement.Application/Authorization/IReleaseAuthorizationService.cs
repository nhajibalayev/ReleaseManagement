using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Authorization;

public static class AuthorizationPolicies
{
    public const string CanCreateRelease = "CanCreateRelease";
    public const string CanReviewRelease = "CanReviewRelease";
    public const string CanPerformPentest = "CanPerformPentest";
    public const string CanPerformInfoSecReview = "CanPerformInfoSecReview";
    public const string CanApproveBusiness = "CanApproveBusiness";
    public const string CanDeployRelease = "CanDeployRelease";
    public const string CanManageSystem = "CanManageSystem";
    public const string CanViewAuditLogs = "CanViewAuditLogs";
}

public enum ReleaseAccessLevel
{
    None = 0,
    View = 1,
    Edit = 2,
    Transition = 3
}

public interface IReleaseAuthorizationService
{
    Task EnsureCanViewAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task EnsureCanEditAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task EnsureCanCreateForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<bool> CanViewAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ReleaseAccessLevel> GetAccessLevelAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);

    bool CanActOnStatus(ReleaseStatus status);
}
