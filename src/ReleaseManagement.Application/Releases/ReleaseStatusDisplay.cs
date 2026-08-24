using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Releases;

public static class ReleaseStatusDisplay
{
    public static string Format(ReleaseStatus status) => status switch
    {
        ReleaseStatus.Draft => "Draft",
        ReleaseStatus.Submitted => "Submitted",
        ReleaseStatus.ReleaseManagerReview => "Release Manager Review",
        ReleaseStatus.ReturnedForRevision => "Returned for Revision",
        ReleaseStatus.PentestReview => "Pentest Review",
        ReleaseStatus.PentestChangesRequired => "Pentest Changes Required",
        ReleaseStatus.InfoSecReview => "InfoSec Review",
        ReleaseStatus.InfoSecChangesRequired => "InfoSec Changes Required",
        ReleaseStatus.BusinessApproval => "Business Approval",
        ReleaseStatus.BusinessChangesRequired => "Business Changes Required",
        ReleaseStatus.Approved => "Approved",
        ReleaseStatus.ReadyForRelease => "Ready for Release",
        ReleaseStatus.DeploymentInProgress => "Deployment In Progress",
        ReleaseStatus.Deployed => "Deployed",
        ReleaseStatus.DeploymentFailed => "Deployment Failed",
        ReleaseStatus.RollbackInProgress => "Rollback In Progress",
        ReleaseStatus.RolledBack => "Rolled Back",
        ReleaseStatus.Rejected => "Rejected",
        ReleaseStatus.Closed => "Closed",
        ReleaseStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string? GetDefaultResponsibleRole(ReleaseStatus status) => status switch
    {
        ReleaseStatus.Draft => RoleNames.ProductOwner,
        ReleaseStatus.Submitted => RoleNames.ReleaseManager,
        ReleaseStatus.ReleaseManagerReview => RoleNames.ReleaseManager,
        ReleaseStatus.ReturnedForRevision => RoleNames.ProductOwner,
        ReleaseStatus.PentestReview => RoleNames.Pentest,
        ReleaseStatus.PentestChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.InfoSecReview => RoleNames.InfoSec,
        ReleaseStatus.InfoSecChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.BusinessApproval => RoleNames.BusinessApprover,
        ReleaseStatus.BusinessChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.Approved => RoleNames.ReleaseManager,
        ReleaseStatus.ReadyForRelease => RoleNames.DevOps,
        ReleaseStatus.DeploymentInProgress => RoleNames.DevOps,
        ReleaseStatus.Deployed => RoleNames.ReleaseManager,
        ReleaseStatus.DeploymentFailed => RoleNames.DevOps,
        ReleaseStatus.RollbackInProgress => RoleNames.DevOps,
        ReleaseStatus.RolledBack => RoleNames.ReleaseManager,
        ReleaseStatus.Rejected => null,
        ReleaseStatus.Closed => null,
        ReleaseStatus.Cancelled => null,
        _ => null
    };

    public static string? GetNextStageDisplay(ReleaseStatus status) => status switch
    {
        ReleaseStatus.Draft => Format(ReleaseStatus.Submitted),
        ReleaseStatus.Submitted => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.ReleaseManagerReview => Format(ReleaseStatus.PentestReview),
        ReleaseStatus.ReturnedForRevision => Format(ReleaseStatus.Submitted),
        ReleaseStatus.PentestReview => Format(ReleaseStatus.InfoSecReview),
        ReleaseStatus.PentestChangesRequired => Format(ReleaseStatus.PentestReview),
        ReleaseStatus.InfoSecReview => Format(ReleaseStatus.BusinessApproval),
        ReleaseStatus.InfoSecChangesRequired => Format(ReleaseStatus.InfoSecReview),
        ReleaseStatus.BusinessApproval => Format(ReleaseStatus.Approved),
        ReleaseStatus.BusinessChangesRequired => Format(ReleaseStatus.BusinessApproval),
        ReleaseStatus.Approved => Format(ReleaseStatus.ReadyForRelease),
        ReleaseStatus.ReadyForRelease => Format(ReleaseStatus.DeploymentInProgress),
        ReleaseStatus.DeploymentInProgress => Format(ReleaseStatus.Deployed),
        ReleaseStatus.DeploymentFailed => Format(ReleaseStatus.RollbackInProgress),
        ReleaseStatus.RollbackInProgress => Format(ReleaseStatus.RolledBack),
        ReleaseStatus.Deployed => Format(ReleaseStatus.Closed),
        _ => null
    };

    public static ApprovalType? MapStatusToApprovalType(ReleaseStatus status) => status switch
    {
        ReleaseStatus.ReleaseManagerReview => ApprovalType.ReleaseManager,
        ReleaseStatus.PentestReview => ApprovalType.Pentest,
        ReleaseStatus.InfoSecReview => ApprovalType.InfoSec,
        ReleaseStatus.BusinessApproval => ApprovalType.Business,
        _ => null
    };

    public static string FormatResponsible(string? role, Guid? userId)
    {
        if (!string.IsNullOrWhiteSpace(role))
        {
            return role switch
            {
                RoleNames.InfoSec => "Information Security",
                RoleNames.ProductOwner => "Product Owner",
                RoleNames.ReleaseManager => "Release Manager",
                RoleNames.BusinessApprover => "Business Approver",
                RoleNames.DevOps => "DevOps",
                RoleNames.Pentest => "Pentest",
                _ => role
            };
        }

        if (!userId.HasValue)
        {
            return "Unassigned";
        }

        var shortId = userId.Value.ToString("N");
        return $"User {shortId.Substring(0, 8)}";
    }
}
