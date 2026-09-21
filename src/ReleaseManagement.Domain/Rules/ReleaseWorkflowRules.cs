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

        // Procedure v4.0 §2 / §5: the Release Manager coordinates the record — checks that the
        // minimum record is complete and starts readiness. RM does not collect domain approvals.
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.ReturnedForRevision,
            true,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReleaseManagerReview,
            ReleaseStatus.ReadinessInProgress,
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

        // Readiness: evidence owners set control statuses (not transitions). RM verifies and
        // marks the release ready only when every applicable control is closed (§8.1 hard block).
        Add(
            ReleaseStatus.ReadinessInProgress,
            ReleaseStatus.ReadyForRelease,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReadinessInProgress,
            ReleaseStatus.ReturnedForRevision,
            true,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReadinessInProgress,
            ReleaseStatus.Rejected,
            true,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReadinessInProgress,
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

        // Legacy in-app structure reviews kept only for releases already in those statuses.
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

        // Legacy: releases approved under the old model continue into readiness.
        Add(
            ReleaseStatus.Approved,
            ReleaseStatus.ReadinessInProgress,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.ReadyForRelease,
            ReleaseStatus.DeploymentInProgress,
            false,
            RoleNames.DevOps);
        Add(
            ReleaseStatus.ReadyForRelease,
            ReleaseStatus.ReadinessInProgress,
            true,
            RoleNames.ReleaseManager);
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

        // §7.2: Deployed → validation is recorded → Stabilization (gate) → Closed (gate).
        Add(
            ReleaseStatus.Deployed,
            ReleaseStatus.Stabilization,
            false,
            RoleNames.ReleaseManager,
            RoleNames.TechnicalOwner);
        Add(
            ReleaseStatus.Deployed,
            ReleaseStatus.RollbackInProgress,
            true,
            RoleNames.DevOps,
            RoleNames.TechnicalOwner);
        Add(
            ReleaseStatus.Stabilization,
            ReleaseStatus.Closed,
            false,
            RoleNames.ReleaseManager);
        Add(
            ReleaseStatus.Stabilization,
            ReleaseStatus.RollbackInProgress,
            true,
            RoleNames.DevOps,
            RoleNames.TechnicalOwner);

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
