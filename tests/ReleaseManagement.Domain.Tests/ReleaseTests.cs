using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Domain.Rules;

namespace ReleaseManagement.Domain.Tests;

public sealed class ReleaseTests
{
    private static readonly DateTime CreatedDate =
        new(2026, 7, 30, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_CreatesDraftOwnedByProductOwner()
    {
        var creatorId = Guid.NewGuid();

        var release = CreateRelease(creatorId);

        Assert.Equal(ReleaseStatus.Draft, release.CurrentStatus);
        Assert.Equal(creatorId, release.CurrentResponsibleUserId);
        Assert.Equal(RoleNames.ProductOwner, release.CurrentResponsibleRole);
        Assert.Equal(CreatedDate, release.CreatedDate);
    }

    [Fact]
    public void TransitionTo_ChangesStatusAndAppendsHistory()
    {
        var creatorId = Guid.NewGuid();
        var changedDate = CreatedDate.AddHours(1);
        var release = CreateRelease(creatorId);

        release.TransitionTo(
            ReleaseStatus.Submitted,
            [RoleNames.ProductOwner],
            creatorId,
            changedDate,
            null,
            null,
            RoleNames.ReleaseManager);

        var history = Assert.Single(release.StatusHistory);
        Assert.Equal(ReleaseStatus.Submitted, release.CurrentStatus);
        Assert.Equal(changedDate, release.SubmittedDate);
        Assert.Equal(ReleaseStatus.Draft, history.FromStatus);
        Assert.Equal(ReleaseStatus.Submitted, history.ToStatus);
        Assert.Equal(RoleNames.ReleaseManager, history.ResponsibleRole);
    }

    [Fact]
    public void UpdateGeneralInformation_RejectsNonUtcDate()
    {
        var release = CreateRelease(Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => release.UpdateGeneralInformation(
                "Release",
                "Description",
                ReleaseType.Standard,
                ReleasePriority.Medium,
                Guid.NewGuid(),
                DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local),
                "1.0",
                "Business reason",
                "Impact",
                CreatedDate.AddMinutes(1)));
    }

    [Fact]
    public void GetSubmissionErrors_ReturnsNoErrors_ForCompleteProductionRelease()
    {
        var release = CreateRelease(Guid.NewGuid());
        release.AddService(
            new ReleaseService(Guid.NewGuid(), release.Id, Guid.NewGuid()),
            CreatedDate.AddMinutes(1));
        release.UpdateReadiness(
            "All automated and manual tests passed.",
            RiskLevel.High,
            "Deployment affects the payment path.",
            "Deploy through the production pipeline.",
            "Redeploy the previous artifact.",
            "Monitor error rate and latency.",
            "Validate health checks and a smoke transaction.",
            false,
            null,
            CreatedDate.AddMinutes(2));

        var errors = ReleaseReadinessRules.GetSubmissionErrors(
            release,
            isProductionEnvironment: true);

        Assert.Empty(errors);
    }

    [Fact]
    public void Approval_RequiresComment_WhenChangesAreRequested()
    {
        var approval = new ReleaseApproval(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApprovalType.InfoSec,
            1,
            null,
            RoleNames.InfoSec,
            true,
            CreatedDate);

        Assert.Throws<ArgumentException>(
            () => approval.Decide(
                ApprovalStatus.ChangesRequired,
                Guid.NewGuid(),
                CreatedDate.AddHours(1),
                null));
    }

    private static Release CreateRelease(Guid creatorId)
    {
        return new Release(
            Guid.NewGuid(),
            "REL-2026-000001",
            "Payment service release",
            "Deploy payment service version 1.0.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedDate.AddDays(3),
            creatorId,
            CreatedDate);
    }
}
