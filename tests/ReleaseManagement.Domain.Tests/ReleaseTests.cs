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

        release.AddReference(
            new ReleaseReference(
                Guid.NewGuid(),
                release.Id,
                ReleaseReferenceType.WorkItem,
                "12345",
                "https://devops.local/wi/12345",
                "Payments feature",
                Guid.NewGuid(),
                CreatedDate.AddMinutes(3)),
            CreatedDate.AddMinutes(3));

        var errors = ReleaseReadinessRules.GetSubmissionErrors(
            release,
            isProductionEnvironment: true);

        Assert.Empty(errors);
    }

    [Fact]
    public void GetSubmissionErrors_RequiresSourceReference()
    {
        var release = CreateRelease(Guid.NewGuid());
        release.AddService(
            new ReleaseService(Guid.NewGuid(), release.Id, Guid.NewGuid()),
            CreatedDate.AddMinutes(1));
        release.UpdateReadiness(
            "Tests passed.",
            RiskLevel.Low,
            string.Empty,
            "Deploy.",
            "Rollback.",
            "Monitor.",
            "Validate.",
            false,
            null,
            CreatedDate.AddMinutes(2));

        var errors = ReleaseReadinessRules.GetSubmissionErrors(release, isProductionEnvironment: false);

        Assert.Contains(errors, error => error.Contains("source reference", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Classification_AnyMajorCriterionMakesReleaseMajor()
    {
        var release = CreateRelease(Guid.NewGuid());

        release.UpdateClassification(
            ClassificationCriteria.ModerateCustomerImpact | ClassificationCriteria.ComplexRecovery,
            SecurityTriggers.None,
            ExecutionMode.Planned,
            null,
            null,
            CreatedDate.AddMinutes(1));

        Assert.Equal(ReleaseCategory.Major, release.Category);
    }

    [Fact]
    public void Classification_DatabaseChangesRaiseToNormal()
    {
        var release = CreateRelease(Guid.NewGuid());
        var service = new ReleaseService(Guid.NewGuid(), release.Id, Guid.NewGuid());
        service.SetChangeFlags(databaseChanges: true, configurationChanges: false, notes: null);

        release.AddService(service, CreatedDate.AddMinutes(1));

        Assert.Equal(ReleaseCategory.Normal, release.Category);
    }

    [Fact]
    public void UpdateClassification_RequiresJustificationForExpedited()
    {
        var release = CreateRelease(Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => release.UpdateClassification(
                ClassificationCriteria.None,
                SecurityTriggers.None,
                ExecutionMode.Expedited,
                " ",
                null,
                CreatedDate.AddMinutes(1)));
    }

    [Fact]
    public void ReadyForReleaseErrors_ReportOpenControls()
    {
        var release = CreateRelease(Guid.NewGuid());
        release.AddService(
            new ReleaseService(Guid.NewGuid(), release.Id, Guid.NewGuid()),
            CreatedDate.AddMinutes(1));
        release.AddReadinessControl(
            new ReadinessControl(
                Guid.NewGuid(),
                release.Id,
                ReadinessControlType.SecurityReadiness,
                RoleNames.InfoSec,
                isRequired: true,
                CreatedDate.AddMinutes(1)));

        var errors = ReleaseReadinessRules.GetReadyForReleaseErrors(release, isProductionEnvironment: false);

        Assert.Contains(errors, error => error.Contains("Security / pentest readiness", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadinessControl_RequiresJustificationForNotRequired()
    {
        var control = new ReadinessControl(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReadinessControlType.OperationalReadiness,
            RoleNames.ITOperations,
            isRequired: true,
            CreatedDate);

        Assert.Throws<ArgumentException>(
            () => control.SetStatus(ReadinessControlStatus.NotRequired, null, null, Guid.NewGuid(), CreatedDate));
    }

    [Fact]
    public void ClosureErrors_RequireStabilizationAndOutcome()
    {
        var release = CreateRelease(Guid.NewGuid());

        var errors = ReleaseReadinessRules.GetClosureErrors(release, null, null, CreatedDate);

        Assert.Contains(errors, error => error.Contains("stabilization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("outcome", StringComparison.OrdinalIgnoreCase));
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
