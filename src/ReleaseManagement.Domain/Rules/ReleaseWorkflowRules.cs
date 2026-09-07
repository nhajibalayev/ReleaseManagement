using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Exceptions;

namespace ReleaseManagement.Domain.Rules;

public static class ReleaseWorkflowRules
{
    private static readonly IReadOnlyDictionary<ReleaseStatus, IReadOnlyCollection<WorkflowTransition>>
        Transitions = BuildTransitions();

    public static bool CanTransition(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        IReadOnlyCollection<string> actorRoles)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);

        if (!Transitions.TryGetValue(currentStatus, out var candidates))
        {
            return false;
        }

        var transition = candidates.SingleOrDefault(item => item.TargetStatus == targetStatus);

        return transition is not null && HasRequiredRole(transition, actorRoles);
    }

    public static void EnsureCanTransition(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        IReadOnlyCollection<string> actorRoles,
        string? comment)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);

        if (!Transitions.TryGetValue(currentStatus, out var candidates))
        {
            throw new InvalidReleaseTransitionException(
                currentStatus,
                targetStatus,
                "The current status is terminal.");
        }

        var transition = candidates.SingleOrDefault(item => item.TargetStatus == targetStatus);

        if (transition is null)
        {
            throw new InvalidReleaseTransitionException(
                currentStatus,
                targetStatus,
                "The transition is not part of the workflow.");
        }

        if (!HasRequiredRole(transition, actorRoles))
        {
            throw new InvalidReleaseTransitionException(
                currentStatus,
                targetStatus,
                "The actor does not have a required role.");
        }

        if (transition.RequiresComment && string.IsNullOrWhiteSpace(comment))
        {
            throw new InvalidReleaseTransitionException(
                currentStatus,
                targetStatus,
                "A comment is required.");
        }
    }

    public static IReadOnlyCollection<ReleaseStatus> GetAllowedTargets(
        ReleaseStatus currentStatus,
        IReadOnlyCollection<string> actorRoles)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);

        if (!Transitions.TryGetValue(currentStatus, out var candidates))
        {
            return [];
        }

        return candidates
            .Where(transition => HasRequiredRole(transition, actorRoles))
            .Select(transition => transition.TargetStatus)
            .ToArray();
    }

    public static bool IsTerminal(ReleaseStatus status)
    {
        return status is
            ReleaseStatus.RolledBack or
            ReleaseStatus.Rejected or
            ReleaseStatus.Closed or
            ReleaseStatus.Cancelled;
    }

    public static bool RequiresComment(ReleaseStatus currentStatus, ReleaseStatus targetStatus)
    {
        return Transitions.TryGetValue(currentStatus, out var candidates) &&
               candidates.SingleOrDefault(item => item.TargetStatus == targetStatus)?.RequiresComment == true;
    }

    private static bool HasRequiredRole(
        WorkflowTransition transition,
        IReadOnlyCollection<string> actorRoles)
    {
        if (actorRoles.Any(role =>
                string.Equals(role, RoleNames.Administrator, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return actorRoles.Any(actorRole =>
            transition.AllowedRoles.Contains(actorRole, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyDictionary<ReleaseStatus, IReadOnlyCollection<WorkflowTransition>>
        BuildTransitions()
    {
        var transitions = new Dictionary<ReleaseStatus, List<WorkflowTransition>>();

        Add(ReleaseStatus.Draft, ReleaseStatus.Submitted, false, RoleNames.ProductOwner);
        Add(
            ReleaseStatus.Draft,
            ReleaseStatus.Cancelled,
            true,
            RoleNames.ProductOwner,
            RoleNames.ReleaseManager);

        Add(
            ReleaseStatus.Submitted,
            ReleaseStatus.ReleaseManagerReview,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.Submitted,
            ReleaseStatus.Cancelled,
            true,
            RoleNames.ProductOwner,
            RoleNames.ReleaseManager);

        // RM orchestrates: send to ONE structure, return to team, finish, or reject.
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.ReturnedForRevision,
            true,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.QaReview,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.InfoSecReview,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.RiskReview,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.ChapterLeadReview,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.Approved,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.Rejected,
            true,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.Cancelled,
            true,
            RoleNames.ReleaseManager);

        Add(
            ReleaseStatus.ReturnedForRevision,
            ReleaseStatus.Submitted,
            false,
            RoleNames.ProductOwner);
        Add(
            ReleaseStatus.ReturnedForRevision,
            ReleaseStatus.Cancelled,
            true,
            RoleNames.ProductOwner,
            RoleNames.ReleaseManager);

        // Each structure: approve → back to RM; changes → team; reject → rejected.
        AddStructureReview(
            ReleaseStatus.QaReview,
            ReleaseStatus.QaChangesRequired,
            RoleNames.QA);
        AddStructureReview(
            ReleaseStatus.InfoSecReview,
            ReleaseStatus.InfoSecChangesRequired,
            RoleNames.InfoSec);
        AddStructureReview(
            ReleaseStatus.RiskReview,
            ReleaseStatus.RiskChangesRequired,
            RoleNames.Risk);
        AddStructureReview(
            ReleaseStatus.ChapterLeadReview,
            ReleaseStatus.ChapterLeadChangesRequired,
            RoleNames.ChapterLead);

        // Legacy pentest/business kept for older releases in DB.
        AddStructureReview(
            ReleaseStatus.PentestReview,
            ReleaseStatus.PentestChangesRequired,
            RoleNames.Pentest);
        Add(
            ReleaseStatus.BusinessApproval,
            ReleaseStatus.ReleaseManagerReview,
            false,
            RoleNames.BusinessApprover);
        Add(
            ReleaseStatus.BusinessApproval,
            ReleaseStatus.BusinessChangesRequired,
            true,
            RoleNames.BusinessApprover);
        Add(
            ReleaseStatus.BusinessApproval,
            ReleaseStatus.Rejected,
            true,
            RoleNames.BusinessApprover);
        Add(
            ReleaseStatus.BusinessChangesRequired,
            ReleaseStatus.BusinessApproval,
            false,
            RoleNames.ProductOwner);

        Add(
            ReleaseStatus.Approved,
            ReleaseStatus.ReadyForRelease,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReadyForRelease,
            ReleaseStatus.DeploymentInProgress,
            false,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.ReadyForRelease,
            ReleaseStatus.Cancelled,
            true,
            RoleNames.ReleaseManager);

        Add(
            ReleaseStatus.DeploymentInProgress,
            ReleaseStatus.Deployed,
            false,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.DeploymentInProgress,
            ReleaseStatus.DeploymentFailed,
            true,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.DeploymentFailed,
            ReleaseStatus.DeploymentInProgress,
            true,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.DeploymentFailed,
            ReleaseStatus.RollbackInProgress,
            true,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.RollbackInProgress,
            ReleaseStatus.RolledBack,
            true,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.Deployed,
            ReleaseStatus.Closed,
            false,
            RoleNames.ReleaseManager);

        return transitions.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<WorkflowTransition>)pair.Value.AsReadOnly());

        void AddStructureReview(
            ReleaseStatus review,
            ReleaseStatus changesRequired,
            string role)
        {
            Add(review, ReleaseStatus.ReleaseManagerReview, false, role);
            Add(review, changesRequired, true, role);
            Add(review, ReleaseStatus.Rejected, true, role);
            Add(changesRequired, review, false, RoleNames.ProductOwner);
            Add(changesRequired, ReleaseStatus.ReturnedForRevision, true, RoleNames.ProductOwner);
        }

        void Add(
            ReleaseStatus from,
            ReleaseStatus to,
            bool requiresComment,
            params string[] allowedRoles)
        {
            if (!transitions.TryGetValue(from, out var list))
            {
                list = [];
                transitions[from] = list;
            }

            list.Add(new WorkflowTransition(to, allowedRoles, requiresComment));
        }
    }

    private sealed record WorkflowTransition(
        ReleaseStatus TargetStatus,
        IReadOnlyCollection<string> AllowedRoles,
        bool RequiresComment);
}
