using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Rules;

/// <summary>
/// Procedure v4.0 §4.1 — any significant Major criterion makes the whole release Major;
/// otherwise Normal impact makes it Normal; otherwise Minor.
/// </summary>
public static class ReleaseClassificationRules
{
    public const ClassificationCriteria MajorCriteria =
        ClassificationCriteria.SignificantCustomerImpact |
        ClassificationCriteria.DowntimeOnCriticalService |
        ClassificationCriteria.MultiTeamCoordination |
        ClassificationCriteria.ComplexDataMigration |
        ClassificationCriteria.ComplexRecovery |
        ClassificationCriteria.HighBusinessSignificance;

    public const ClassificationCriteria NormalCriteria =
        ClassificationCriteria.ModerateCustomerImpact |
        ClassificationCriteria.LimitedDowntime |
        ClassificationCriteria.SchemaOrDataChange |
        ClassificationCriteria.SharedComponentImpact;

    public static ReleaseCategory Classify(
        ClassificationCriteria criteria,
        bool hasDatabaseChanges,
        bool downtimeRequired)
    {
        if ((criteria & MajorCriteria) != ClassificationCriteria.None)
        {
            return ReleaseCategory.Major;
        }

        if ((criteria & NormalCriteria) != ClassificationCriteria.None ||
            hasDatabaseChanges ||
            downtimeRequired)
        {
            return ReleaseCategory.Normal;
        }

        return ReleaseCategory.Minor;
    }

    public static bool IsMajorCriterion(ClassificationCriteria criterion) =>
        (criterion & MajorCriteria) != ClassificationCriteria.None;

    /// <summary>§5.3 — the minimum recovery approach allowed for a category.</summary>
    public static bool IsRecoveryApproachSufficient(ReleaseCategory category, RecoveryApproach approach) =>
        category switch
        {
            ReleaseCategory.Major => approach == RecoveryApproach.FormalRecoveryPlan,
            ReleaseCategory.Normal => approach is
                RecoveryApproach.RollbackOrBackout or
                RecoveryApproach.RollForward or
                RecoveryApproach.FormalRecoveryPlan,
            _ => true
        };
}
