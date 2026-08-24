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
        ReleaseStatus.PentestReview,
        RoleNames.ReleaseManager)]
    [InlineData(
        ReleaseStatus.PentestReview,
        ReleaseStatus.InfoSecReview,
        RoleNames.Pentest)]
    [InlineData(
        ReleaseStatus.InfoSecReview,
        ReleaseStatus.BusinessApproval,
        RoleNames.InfoSec)]
    [InlineData(
        ReleaseStatus.BusinessApproval,
        ReleaseStatus.Approved,
        RoleNames.BusinessApprover)]
    [InlineData(
        ReleaseStatus.ReadyForRelease,
        ReleaseStatus.DeploymentInProgress,
        RoleNames.DevOps)]
    [InlineData(
        ReleaseStatus.Deployed,
        ReleaseStatus.Closed,
        RoleNames.ReleaseManager)]
    public void CanTransition_ReturnsTrue_ForDefinedTransitionAndRole(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        string role)
    {
        var result = ReleaseWorkflowRules.CanTransition(currentStatus, targetStatus, [role]);

        Assert.True(result);
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
