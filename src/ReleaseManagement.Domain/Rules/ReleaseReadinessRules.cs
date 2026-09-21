using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Domain.Rules;

public sealed record ReadinessControlDefinition(
    ReadinessControlType ControlType,
    string OwnerRole,
    IReadOnlyCollection<string> AllowedRoles,
    string Title,
    string ProcedureSection,
    string Description);

/// <summary>
/// Procedure v4.0 §5 / §8.1 — readiness control table and the hard gates that
/// block Ready-for-release and Closed while mandatory evidence is missing.
/// </summary>
public static class ReleaseReadinessRules
{
    public static readonly IReadOnlyList<ReadinessControlDefinition> Definitions =
    [
        new(
            ReadinessControlType.SourceTraceability,
            RoleNames.ReleaseManager,
            [RoleNames.ReleaseManager, RoleNames.ProductOwner],
            "Source work item / Change",
            "§5, §8.1",
            "Azure Boards WI / PR / pipeline / artifact (Application) or Service Desk Change (Infrastructure) is linked."),
        new(
            ReadinessControlType.ProductBusinessReadiness,
            RoleNames.ProductOwner,
            [RoleNames.ProductOwner],
            "Product / business readiness",
            "§5, §2",
            "Product Owner confirms business readiness (formal UAT only when separately required)."),
        new(
            ReadinessControlType.QaTechnicalValidation,
            RoleNames.QA,
            [RoleNames.QA],
            "QA / technical validation",
            "§5",
            "QA evidence per the QA Framework — link or reference to test results."),
        new(
            ReadinessControlType.SecurityReadiness,
            RoleNames.InfoSec,
            [RoleNames.InfoSec],
            "Security / pentest readiness",
            "§5.2",
            "InfoSec sets Not Required / Ready / Ready with Approved Exception / Blocked."),
        new(
            ReadinessControlType.DatabaseMigrationReadiness,
            RoleNames.DBA,
            [RoleNames.DBA, RoleNames.TechnicalOwner],
            "DB / migration readiness",
            "§5.4",
            "Migration scripts, technical validation, recovery approach and source-target reconciliation."),
        new(
            ReadinessControlType.OperationalReadiness,
            RoleNames.ITOperations,
            [RoleNames.ITOperations, RoleNames.TechnicalOwner],
            "Operational readiness",
            "§5",
            "Service health, production support and monitoring readiness when operations are affected."),
        new(
            ReadinessControlType.RecoveryReadiness,
            RoleNames.TechnicalOwner,
            [RoleNames.TechnicalOwner],
            "Recovery readiness",
            "§5.3",
            "Rollback / roll-forward approach matching the category; formal plan for Major."),
        new(
            ReadinessControlType.WindowAndCommunication,
            RoleNames.ReleaseManager,
            [RoleNames.ReleaseManager],
            "Window and communication",
            "§6.1, §6.4",
            "Approved window (and maintenance window when required) plus impact-based stakeholder communication.")
    ];

    public static ReadinessControlDefinition GetDefinition(ReadinessControlType controlType) =>
        Definitions.Single(item => item.ControlType == controlType);

    /// <summary>Which controls apply to the release (§5: "applicable controls").</summary>
    public static bool IsControlRequired(Release release, ReadinessControlType controlType)
    {
        ArgumentNullException.ThrowIfNull(release);

        return controlType switch
        {
            ReadinessControlType.DatabaseMigrationReadiness =>
                release.HasDatabaseChanges ||
                release.ClassificationCriteria.HasFlag(ClassificationCriteria.SchemaOrDataChange) ||
                release.ClassificationCriteria.HasFlag(ClassificationCriteria.ComplexDataMigration),
            ReadinessControlType.OperationalReadiness =>
                release.OperationalImpact ||
                release.DowntimeRequired ||
                release.Category == ReleaseCategory.Major,
            _ => true
        };
    }

    public static bool CanUserSetControl(
        ReadinessControlDefinition definition,
        IReadOnlyCollection<string> actorRoles)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(actorRoles);

        if (actorRoles.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return definition.AllowedRoles.Any(role => actorRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Submission checks (§8.2 minimum record). The Product Owner fills these in the wizard.</summary>
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

        if (release.PlannedWindowEnd <= release.PlannedWindowStart)
        {
            errors.Add("A planned deployment window (start and end) is required (§3.2).");
        }

        if (!release.References.Any(item => item.IsSourceReference))
        {
            errors.Add("At least one source reference (work item, PR, pipeline, artifact or Service Desk Change) is required (§1.3, §8.1).");
        }

        if (release.IsExpedited && string.IsNullOrWhiteSpace(release.ExpeditedJustification))
        {
            errors.Add("Expedited execution requires an urgency justification (§6.2).");
        }

        if (isProductionEnvironment &&
            release.Category != ReleaseCategory.Minor &&
            string.IsNullOrWhiteSpace(release.RollbackPlan))
        {
            errors.Add("A rollback / backout or roll-forward description is required for Normal and Major production releases (§5.3).");
        }

        if (release.Category == ReleaseCategory.Major)
        {
            if (release.RecoveryApproach != RecoveryApproach.FormalRecoveryPlan)
            {
                errors.Add("Major releases require a formal recovery / backout plan (§5.3).");
            }

            if (string.IsNullOrWhiteSpace(release.RecoveryDecisionPoints) ||
                string.IsNullOrWhiteSpace(release.RecoveryResponsibleParties))
            {
                errors.Add("Major releases must define recovery decision points and responsible parties (§5.3).");
            }
        }
        else if (!ReleaseClassificationRules.IsRecoveryApproachSufficient(release.Category, release.RecoveryApproach))
        {
            errors.Add($"The recovery approach is not sufficient for a {release.Category} release (§5.3).");
        }

        if (release.DowntimeRequired && release.ExpectedDowntimeMinutes is null)
        {
            errors.Add("Expected downtime is required when downtime is planned.");
        }

        if (release.PlannedMaintenance && string.IsNullOrWhiteSpace(release.MaintenanceApprovalReference))
        {
            errors.Add("Planned maintenance requires the approved maintenance window reference (§6.1).");
        }

        if (RequiresMaintenanceWindow(release) && !release.PlannedMaintenance)
        {
            errors.Add("Planned downtime, critical customer-facing impact, shared/core impact or multi-team deployment requires an approved maintenance window — set Planned Maintenance = Yes (§6.1).");
        }

        return errors.AsReadOnly();
    }

    /// <summary>§6.1 — when an approved maintenance window is mandatory.</summary>
    public static bool RequiresMaintenanceWindow(Release release)
    {
        ArgumentNullException.ThrowIfNull(release);

        return release.DowntimeRequired ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.DowntimeOnCriticalService) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.SharedComponentImpact) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.MultiTeamCoordination);
    }

    /// <summary>§6.4 — when a pre-release stakeholder notification is mandatory.</summary>
    public static bool RequiresPreReleaseCommunication(Release release)
    {
        ArgumentNullException.ThrowIfNull(release);

        return release.DowntimeRequired ||
               release.OperationalImpact ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.SignificantCustomerImpact) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.ModerateCustomerImpact) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.MultiTeamCoordination);
    }

    /// <summary>§7.2 — business validation is required for business-visible changes.</summary>
    public static bool RequiresBusinessValidation(Release release)
    {
        ArgumentNullException.ThrowIfNull(release);

        return release.ClassificationCriteria.HasFlag(ClassificationCriteria.SignificantCustomerImpact) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.ModerateCustomerImpact) ||
               release.ClassificationCriteria.HasFlag(ClassificationCriteria.HighBusinessSignificance);
    }

    /// <summary>§7.3 — PIR triggers derived from the release state.</summary>
    public static PirTriggers GetPirTriggers(Release release, PostReleaseValidation? validation)
    {
        ArgumentNullException.ThrowIfNull(release);

        var triggers = PirTriggers.None;

        if (release.IsExpedited)
        {
            triggers |= PirTriggers.Expedited;
        }

        if (release.CurrentStatus is ReleaseStatus.DeploymentFailed ||
            release.Outcome == ReleaseOutcome.Failed ||
            release.DeploymentRecords.Any(item => item.Status == DeploymentStatus.Failed))
        {
            triggers |= PirTriggers.FailedDeployment;
        }

        if (validation is { IsComplete: true, IsPassed: false })
        {
            triggers |= PirTriggers.FailedValidation;
        }

        if (release.CurrentStatus is ReleaseStatus.RollbackInProgress or ReleaseStatus.RolledBack ||
            release.Outcome is ReleaseOutcome.RolledBack or ReleaseOutcome.Remediated)
        {
            triggers |= PirTriggers.RollbackOrRemediation;
        }

        if (release.References.Any(item => item.ReferenceType == ReleaseReferenceType.Incident))
        {
            triggers |= PirTriggers.ReleaseRelatedIncident;
        }

        return triggers;
    }

    /// <summary>§8.1 hard block — Ready-for-release gate.</summary>
    public static IReadOnlyCollection<string> GetReadyForReleaseErrors(
        Release release,
        bool isProductionEnvironment)
    {
        ArgumentNullException.ThrowIfNull(release);

        var errors = new List<string>(GetSubmissionErrors(release, isProductionEnvironment));

        foreach (var definition in Definitions)
        {
            var required = IsControlRequired(release, definition.ControlType);
            if (!required)
            {
                continue;
            }

            var control = release.ReadinessControls.SingleOrDefault(item => item.ControlType == definition.ControlType);
            if (control is null)
            {
                errors.Add($"Readiness control '{definition.Title}' has not been initialised.");
                continue;
            }

            if (control.Status == ReadinessControlStatus.Blocked)
            {
                errors.Add($"'{definition.Title}' is Blocked: {control.Justification}");
                continue;
            }

            if (!control.IsClosed)
            {
                errors.Add($"'{definition.Title}' is still {control.Status} (owner: {definition.OwnerRole}).");
                continue;
            }

            if (definition.ControlType == ReadinessControlType.SecurityReadiness &&
                control.Status == ReadinessControlStatus.NotRequired &&
                release.SecurityTriggers != SecurityTriggers.None)
            {
                errors.Add("Security readiness cannot be Not Required while pentest / assessment triggers are selected (§5.2).");
            }
        }

        if (release.IsExpedited)
        {
            var productReadiness = release.ReadinessControls.SingleOrDefault(
                item => item.ControlType == ReadinessControlType.ProductBusinessReadiness);

            if (productReadiness is null || productReadiness.Status != ReadinessControlStatus.Ready)
            {
                errors.Add("Expedited Application releases require explicit Product Owner confirmation (§6.2).");
            }
        }

        if (RequiresPreReleaseCommunication(release) &&
            !release.Communications.Any(item => item.CommunicationType == CommunicationType.PreRelease))
        {
            errors.Add("A pre-release stakeholder notification must be logged before the release is ready (§6.4).");
        }

        if (release.Category == ReleaseCategory.Major &&
            !release.IsExpedited &&
            release.ForecastId is null)
        {
            errors.Add("Major planned releases must be listed in the quarterly Release Forecast (§3.1).");
        }

        return errors.AsReadOnly();
    }

    /// <summary>§7.2 — Stabilization gate: validation must be recorded and passed.</summary>
    public static IReadOnlyCollection<string> GetStabilizationErrors(
        Release release,
        PostReleaseValidation? validation)
    {
        ArgumentNullException.ThrowIfNull(release);

        var errors = new List<string>();

        if (validation is null || !validation.IsComplete)
        {
            errors.Add("Post-release validation (technical, and business when required) must be recorded first (§7.2).");
        }
        else if (!validation.IsPassed)
        {
            errors.Add("Post-release validation did not pass — record a rollback / remediation instead of stabilization (§7.2).");
        }

        if (RequiresBusinessValidation(release) && validation is { BusinessValidationRequired: false })
        {
            errors.Add("Business validation is required for business-visible changes (§7.2).");
        }

        return errors.AsReadOnly();
    }

    /// <summary>§7.2 / §7.3 / §8.1 — Closed gate.</summary>
    public static IReadOnlyCollection<string> GetClosureErrors(
        Release release,
        PostReleaseValidation? validation,
        PostImplementationReview? review,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(release);

        var errors = new List<string>(GetStabilizationErrors(release, validation));

        if (release.StabilizationEnd is null)
        {
            errors.Add("A stabilization / monitoring period must be recorded (§7.2).");
        }
        else if (release.StabilizationEnd > nowUtc)
        {
            errors.Add($"The stabilization period ends {release.StabilizationEnd:u}; the release cannot be closed earlier (§7.2).");
        }

        if (release.Outcome is null)
        {
            errors.Add("The final outcome (Successful / Remediated / Failed) must be recorded before closure (§8.2).");
        }

        var triggers = GetPirTriggers(release, validation);
        if (triggers != PirTriggers.None && (review is null || review.Status != PirStatus.Completed))
        {
            errors.Add($"A Post-Release Review is mandatory ({triggers}) and must be completed before closure (§7.3).");
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
