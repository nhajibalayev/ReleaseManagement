using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Tests;

public sealed class ReleaseStatusDisplayTests
{
    [Theory]
    [InlineData(ReleaseStatus.InfoSecReview, "InfoSec Review", "Information Security")]
    [InlineData(ReleaseStatus.ReturnedForRevision, "Returned to Team", "Product Owner")]
    [InlineData(ReleaseStatus.ReadyForRelease, "Ready for Release", "DevOps")]
    [InlineData(ReleaseStatus.QaReview, "QA Review", "QA")]
    [InlineData(ReleaseStatus.RiskReview, "Risk Review", "Risk")]
    [InlineData(ReleaseStatus.ChapterLeadReview, "Chapter Lead Review", "Chapter Lead")]
    [InlineData(ReleaseStatus.ReadinessInProgress, "Readiness In Progress", "Release Manager")]
    [InlineData(ReleaseStatus.Stabilization, "Stabilization", "Release Manager")]
    public void Formats_status_and_responsible_role(
        ReleaseStatus status,
        string expectedStatus,
        string expectedRoleDisplay)
    {
        Assert.Equal(expectedStatus, ReleaseStatusDisplay.Format(status));

        var role = ReleaseStatusDisplay.GetDefaultResponsibleRole(status);
        Assert.Equal(expectedRoleDisplay, ReleaseStatusDisplay.FormatResponsible(role, null));
    }

    [Fact]
    public void Next_stage_after_structure_review_returns_to_rm()
    {
        Assert.Equal(
            "Release Manager Review",
            ReleaseStatusDisplay.GetNextStageDisplay(ReleaseStatus.InfoSecReview));
        Assert.Equal(
            "Release Manager Review",
            ReleaseStatusDisplay.GetNextStageDisplay(ReleaseStatus.QaReview));
    }

    [Fact]
    public void Workflow_service_delegates_transition_rules()
    {
        // Smoke-check the public contract used by controllers without infrastructure.
        var allowed = Domain.Rules.ReleaseWorkflowRules.CanTransition(
            ReleaseStatus.Draft,
            ReleaseStatus.Submitted,
            [RoleNames.ProductOwner]);

        Assert.True(allowed);
        Assert.Equal(RoleNames.ReleaseManager, ReleaseStatusDisplay.GetDefaultResponsibleRole(ReleaseStatus.Submitted));
    }
}
