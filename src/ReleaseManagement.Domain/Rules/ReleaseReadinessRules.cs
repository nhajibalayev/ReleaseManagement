using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Rules;

public static class ReleaseReadinessRules
{
    public static IReadOnlyCollection<string> GetSubmissionErrors(
        Release release,
        bool isProductionEnvironment)
    {
        ArgumentNullException.ThrowIfNull(release);

        var errors = new List<string>();

        AddRequired(errors, release.Title, "Title is required.");
        AddRequired(errors, release.Description, "Description is required.");
        AddRequired(errors, release.TestingSummary, "Testing summary is required.");

        if (release.Services.Count == 0)
        {
            errors.Add("At least one service is required.");
        }

        if (isProductionEnvironment && string.IsNullOrWhiteSpace(release.RollbackPlan))
        {
            errors.Add("Rollback plan is required for production releases.");
        }

        if (release.RiskLevel is RiskLevel.High or RiskLevel.Critical &&
            string.IsNullOrWhiteSpace(release.RiskDescription))
        {
            errors.Add("Risk description is required for high or critical risk.");
        }

        if (release.DowntimeRequired && release.ExpectedDowntimeMinutes is null)
        {
            errors.Add("Expected downtime is required when downtime is planned.");
        }

        return errors.AsReadOnly();
    }

    public static bool HasRequiredApprovals(IEnumerable<ReleaseApproval> approvals)
    {
        ArgumentNullException.ThrowIfNull(approvals);

        var requiredApprovals = approvals.Where(approval => approval.IsRequired).ToArray();

        return requiredApprovals.Length > 0 &&
               requiredApprovals.All(approval => approval.Status == ApprovalStatus.Approved);
    }

    private static void AddRequired(ICollection<string> errors, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }
}
