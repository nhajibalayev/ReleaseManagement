using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Exceptions;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Domain.Tests;

public sealed class ReleaseWorkflowRulesTests
{
    [Theory]
    [InlineData(ReleaseStatus.Draft, ReleaseStatus.Submitted, RoleNames.ProductOwner)]
    [InlineData(
        ReleaseStatus.ReleaseManagerReview,
        ReleaseStatus.ReadinessInProgress,
        RoleNames.ReleaseManager)]
    [InlineData(
        ReleaseStatus.ReleaseManagerReview,
        ReleaseStatus.ReturnedForRevision,
        RoleNames.ReleaseManager)]
    [InlineData(
        ReleaseStatus.ReadinessInProgress,
        ReleaseStatus.ReadyForRelease,
        RoleNames.ReleaseManager)]
    [InlineData(
        ReleaseStatus.Deployed,
        ReleaseStatus.Stabilization,
        RoleNames.TechnicalOwner)]
    [InlineData(
        ReleaseStatus.Stabilization,
        ReleaseStatus.Closed,
        RoleNames.ReleaseManager)]
    [InlineData(
        ReleaseStatus.QaReview,
        ReleaseStatus.ReleaseManagerReview,
        RoleNames.QA)]
    [InlineData(
        ReleaseStatus.InfoSecReview,
        ReleaseStatus.ReleaseManagerReview,
        RoleNames.InfoSec)]
    [InlineData(
        ReleaseStatus.RiskReview,
        ReleaseStatus.ReleaseManagerReview,
        RoleNames.Risk)]
    [InlineData(
        ReleaseStatus.ChapterLeadReview,
        ReleaseStatus.ReleaseManagerReview,
        RoleNames.ChapterLead)]
    [InlineData(
        ReleaseStatus.ReadyForRelease,
        ReleaseStatus.DeploymentInProgress,
        RoleNames.DevOps)]
    public void CanTransition_ReturnsTrue_ForDefinedTransitionAndRole(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        string role)
    {
        var result = ReleaseWorkflowRules.CanTransition(currentStatus, targetStatus, [role]);

        Assert.True(result);
    }

    [Theory]
    [InlineData(ReleaseStatus.PentestReview)]
    [InlineData(ReleaseStatus.QaReview)]
    [InlineData(ReleaseStatus.InfoSecReview)]
    [InlineData(ReleaseStatus.Approved)]
    public void CanTransition_ReturnsFalse_WhenRmTriesToRouteApprovals(ReleaseStatus target)
    {
        // Procedure v4.0 §2: RM coordinates readiness evidence; it does not route in-app approvals.
        var result = ReleaseWorkflowRules.CanTransition(
            ReleaseStatus.ReleaseManagerReview,
            target,
            [RoleNames.ReleaseManager]);

        Assert.False(result);
    }

    [Fact]
    public void CanTransition_ReturnsFalse_WhenClosingDirectlyFromDeployed()
    {
        // §7.2: validation and stabilization come before closure.
        var result = ReleaseWorkflowRules.CanTransition(
            ReleaseStatus.Deployed,
            ReleaseStatus.Closed,
            [RoleNames.ReleaseManager]);

        Assert.False(result);
    }

    [Fact]
    public void CanTransition_ReturnsFalse_ForUndefinedTransition()
    {
        var result = ReleaseWorkflowRules.CanTransition(
            ReleaseStatus.Draft,
            ReleaseStatus.Deployed,
            [RoleNames.Administrator]);

        Assert.False(result);
    }

    [Fact]
    public void EnsureCanTransition_RejectsActorWithoutRequiredRole()
    {
        var exception = Assert.Throws<InvalidReleaseTransitionException>(
            () => ReleaseWorkflowRules.EnsureCanTransition(
                ReleaseStatus.Draft,
                ReleaseStatus.Submitted,
                [RoleNames.DevOps],
                null));

        Assert.Equal(ReleaseStatus.Draft, exception.CurrentStatus);
        Assert.Equal(ReleaseStatus.Submitted, exception.TargetStatus);
    }

    [Fact]
    public void EnsureCanTransition_RequiresCommentForReturn()
    {
        var exception = Assert.Throws<InvalidReleaseTransitionException>(
            () => ReleaseWorkflowRules.EnsureCanTransition(
                ReleaseStatus.ReleaseManagerReview,
                ReleaseStatus.ReturnedForRevision,
                [RoleNames.ReleaseManager],
                " "));

        Assert.Contains("comment is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ReleaseStatus.RolledBack)]
    [InlineData(ReleaseStatus.Rejected)]
    [InlineData(ReleaseStatus.Closed)]
    [InlineData(ReleaseStatus.Cancelled)]
    public void TerminalStatus_HasNoAllowedTargets(ReleaseStatus status)
    {
        Assert.True(ReleaseWorkflowRules.IsTerminal(status));
        Assert.Empty(
            ReleaseWorkflowRules.GetAllowedTargets(status, [RoleNames.Administrator]));
    }
}
