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
        ReleaseStatus.ReturnedForRevision => "Returned to Team",
        ReleaseStatus.PentestReview => "Pentest Review",
        ReleaseStatus.PentestChangesRequired => "Pentest Changes Required",
        ReleaseStatus.InfoSecReview => "InfoSec Review",
        ReleaseStatus.InfoSecChangesRequired => "InfoSec Changes Required",
        ReleaseStatus.BusinessApproval => "Business Approval",
        ReleaseStatus.BusinessChangesRequired => "Business Changes Required",
        ReleaseStatus.QaReview => "QA Review",
        ReleaseStatus.QaChangesRequired => "QA Changes Required",
        ReleaseStatus.RiskReview => "Risk Review",
        ReleaseStatus.RiskChangesRequired => "Risk Changes Required",
        ReleaseStatus.ChapterLeadReview => "Chapter Lead Review",
        ReleaseStatus.ChapterLeadChangesRequired => "Chapter Lead Changes Required",
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
        ReleaseStatus.ReadinessInProgress => "Readiness In Progress",
        ReleaseStatus.Stabilization => "Stabilization",
        _ => status.ToString()
    };

    public static string FormatAction(ReleaseStatus target) => target switch
    {
        ReleaseStatus.QaReview => "Send to QA",
        ReleaseStatus.InfoSecReview => "Send to InfoSec",
        ReleaseStatus.RiskReview => "Send to Risk",
        ReleaseStatus.ChapterLeadReview => "Send to Chapter Lead",
        ReleaseStatus.ReturnedForRevision => "Return to team",
        ReleaseStatus.ReadinessInProgress => "Accept record and start readiness",
        ReleaseStatus.Stabilization => "Start stabilization period",
        ReleaseStatus.Approved => "Mark as approved (legacy)",
        ReleaseStatus.Rejected => "Reject release",
        ReleaseStatus.Cancelled => "Cancel release",
        ReleaseStatus.ReleaseManagerReview => "Return to Release Manager",
        ReleaseStatus.Submitted => "Resubmit to Release Manager",
        ReleaseStatus.ReadyForRelease => "Mark ready for release (all controls closed)",
        ReleaseStatus.DeploymentInProgress => "Start deployment",
        ReleaseStatus.Deployed => "Mark deployed",
        ReleaseStatus.DeploymentFailed => "Mark deployment failed",
        ReleaseStatus.RollbackInProgress => "Start rollback",
        ReleaseStatus.RolledBack => "Mark rolled back",
        ReleaseStatus.Closed => "Close release",
        ReleaseStatus.QaChangesRequired => "Request QA changes",
        ReleaseStatus.InfoSecChangesRequired => "Request InfoSec changes",
        ReleaseStatus.RiskChangesRequired => "Request Risk changes",
        ReleaseStatus.ChapterLeadChangesRequired => "Request Chapter Lead changes",
        _ => Format(target)
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
        ReleaseStatus.QaReview => RoleNames.QA,
        ReleaseStatus.QaChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.RiskReview => RoleNames.Risk,
        ReleaseStatus.RiskChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.ChapterLeadReview => RoleNames.ChapterLead,
        ReleaseStatus.ChapterLeadChangesRequired => RoleNames.ProductOwner,
        ReleaseStatus.Approved => RoleNames.ReleaseManager,
        ReleaseStatus.ReadinessInProgress => RoleNames.ReleaseManager,
        ReleaseStatus.Stabilization => RoleNames.ReleaseManager,
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
        ReleaseStatus.ReleaseManagerReview => Format(ReleaseStatus.ReadinessInProgress),
        ReleaseStatus.ReadinessInProgress => Format(ReleaseStatus.ReadyForRelease),
        ReleaseStatus.Stabilization => Format(ReleaseStatus.Closed),
        ReleaseStatus.ReturnedForRevision => Format(ReleaseStatus.Submitted),
        ReleaseStatus.QaReview => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.QaChangesRequired => Format(ReleaseStatus.QaReview),
        ReleaseStatus.InfoSecReview => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.InfoSecChangesRequired => Format(ReleaseStatus.InfoSecReview),
        ReleaseStatus.RiskReview => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.RiskChangesRequired => Format(ReleaseStatus.RiskReview),
        ReleaseStatus.ChapterLeadReview => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.ChapterLeadChangesRequired => Format(ReleaseStatus.ChapterLeadReview),
        ReleaseStatus.PentestReview => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.PentestChangesRequired => Format(ReleaseStatus.PentestReview),
        ReleaseStatus.BusinessApproval => Format(ReleaseStatus.ReleaseManagerReview),
        ReleaseStatus.BusinessChangesRequired => Format(ReleaseStatus.BusinessApproval),
        ReleaseStatus.Approved => Format(ReleaseStatus.ReadinessInProgress),
        ReleaseStatus.ReadyForRelease => Format(ReleaseStatus.DeploymentInProgress),
        ReleaseStatus.DeploymentInProgress => Format(ReleaseStatus.Deployed),
        ReleaseStatus.DeploymentFailed => Format(ReleaseStatus.RollbackInProgress),
        ReleaseStatus.RollbackInProgress => Format(ReleaseStatus.RolledBack),
        ReleaseStatus.Deployed => Format(ReleaseStatus.Stabilization),
        _ => null
    };

    /// <summary>
    /// Structure reviewers only — RM uses Available actions to route.
    /// </summary>
    public static ApprovalType? MapStatusToApprovalType(ReleaseStatus status) => status switch
    {
        ReleaseStatus.PentestReview => ApprovalType.Pentest,
        ReleaseStatus.InfoSecReview => ApprovalType.InfoSec,
        ReleaseStatus.BusinessApproval => ApprovalType.Business,
        ReleaseStatus.QaReview => ApprovalType.QA,
        ReleaseStatus.RiskReview => ApprovalType.Risk,
        ReleaseStatus.ChapterLeadReview => ApprovalType.ChapterLead,
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
                RoleNames.QA => "QA",
                RoleNames.Risk => "Risk",
                RoleNames.ChapterLead => "Chapter Lead",
                RoleNames.TechnicalOwner => "Technical Owner",
                RoleNames.DBA => "DBA / Data Engineering",
                RoleNames.ITOperations => "IT Operations",
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
