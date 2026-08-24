using ReleaseManagement.Application.Releases;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Tests;

public sealed class ReleaseStatusDisplayTests
{
    [Theory]
    [InlineData(ReleaseStatus.InfoSecReview, "InfoSec Review", "Information Security")]
    [InlineData(ReleaseStatus.ReturnedForRevision, "Returned for Revision", "Product Owner")]
    [InlineData(ReleaseStatus.ReadyForRelease, "Ready for Release", "DevOps")]
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
    public void Next_stage_after_infosec_is_business_approval()
    {
        Assert.Equal(
            "Business Approval",
            ReleaseStatusDisplay.GetNextStageDisplay(ReleaseStatus.InfoSecReview));
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
