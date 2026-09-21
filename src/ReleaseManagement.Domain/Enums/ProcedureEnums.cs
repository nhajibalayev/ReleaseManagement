namespace ReleaseManagement.Domain.Enums;

/// <summary>Procedure v4.0 §1.2 — release direction. Only Application is used today.</summary>
public enum ReleaseTrack
{
    Application = 1,
    Infrastructure = 2
}

/// <summary>Procedure v4.0 §4.1 — release category (drives readiness / recovery depth).</summary>
public enum ReleaseCategory
{
    Minor = 1,
    Normal = 2,
    Major = 3
}

/// <summary>Procedure v4.0 §4.2 / §6.2 — execution mode.</summary>
public enum ExecutionMode
{
    Planned = 1,
    Expedited = 2
}

/// <summary>Procedure v4.0 §5.3 — recovery approach by category.</summary>
public enum RecoveryApproach
{
    StandardPipelineRollback = 1,
    RollbackOrBackout = 2,
    RollForward = 3,
    FormalRecoveryPlan = 4
}

/// <summary>Procedure v4.0 §4.1 — classification criteria (flags). Major criteria are 1..32, Normal criteria 64..512.</summary>
[Flags]
public enum ClassificationCriteria
{
    None = 0,

    // Major criteria — any of them makes the whole release Major.
    SignificantCustomerImpact = 1,
    DowntimeOnCriticalService = 2,
    MultiTeamCoordination = 4,
    ComplexDataMigration = 8,
    ComplexRecovery = 16,
    HighBusinessSignificance = 32,

    // Normal criteria.
    ModerateCustomerImpact = 64,
    LimitedDowntime = 128,
    SchemaOrDataChange = 256,
    SharedComponentImpact = 512
}

/// <summary>Procedure v4.0 §5.2 — pentest / security assessment triggers.</summary>
[Flags]
public enum SecurityTriggers
{
    None = 0,
    NewInternetFacingApplication = 1,
    NewExternalApi = 2,
    SignificantAuthorizationChange = 4,
    SensitiveDataChange = 8,
    SecurityArchitectureChange = 16,
    InfoSecRiskRequest = 32,
    RegulatoryOrContractRequirement = 64
}

/// <summary>Procedure v4.0 §5 — readiness control table.</summary>
public enum ReadinessControlType
{
    SourceTraceability = 1,
    ProductBusinessReadiness = 2,
    QaTechnicalValidation = 3,
    SecurityReadiness = 4,
    DatabaseMigrationReadiness = 5,
    OperationalReadiness = 6,
    RecoveryReadiness = 7,
    WindowAndCommunication = 8
}

/// <summary>Status of a readiness control. NotRequired requires a justification.</summary>
public enum ReadinessControlStatus
{
    Pending = 1,
    NotRequired = 2,
    Ready = 3,
    ReadyWithApprovedException = 4,
    Blocked = 5
}

/// <summary>Procedure v4.0 §1.3 / §8.1 — link-first evidence references.</summary>
public enum ReleaseReferenceType
{
    WorkItem = 1,
    PullRequest = 2,
    Pipeline = 3,
    Artifact = 4,
    ServiceDeskChange = 5,
    Incident = 6,
    TestEvidence = 7,
    SecurityAssessment = 8,
    MonitoringDashboard = 9,
    PostImplementationReview = 10,
    Other = 99
}

/// <summary>Procedure v4.0 §8.2 — final release outcome.</summary>
public enum ReleaseOutcome
{
    Successful = 1,
    Failed = 2,
    RolledBack = 3,
    Remediated = 4,
    Cancelled = 5
}

/// <summary>Procedure v4.0 §6.3 — freeze authority.</summary>
public enum FreezeType
{
    BusinessEnterprise = 1,
    ItTechnical = 2
}

/// <summary>Procedure v4.0 §6.4 — impact-based stakeholder communication.</summary>
public enum CommunicationType
{
    PreRelease = 1,
    StatusUpdate = 2,
    Completion = 3,
    Reschedule = 4,
    Incident = 5
}

/// <summary>Procedure v4.0 §7.2 — validation result.</summary>
public enum ValidationResult
{
    Pending = 1,
    Passed = 2,
    PassedWithIssues = 3,
    Failed = 4
}

/// <summary>Procedure v4.0 §7.3 — PIR triggers (flags).</summary>
[Flags]
public enum PirTriggers
{
    None = 0,
    Expedited = 1,
    FailedDeployment = 2,
    FailedValidation = 4,
    RollbackOrRemediation = 8,
    ReleaseRelatedIncident = 16,
    SignificantImpact = 32,
    SecurityOrDataIssue = 64,
    RepeatedFailure = 128,
    Manual = 256
}

public enum PirFormat
{
    AsyncEvidenceReview = 1,
    Meeting = 2
}

public enum PirStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3
}

/// <summary>Procedure v4.0 §3.1 — quarterly forecast entry status (forecast is not an approval).</summary>
public enum ForecastStatus
{
    Planned = 1,
    Confirmed = 2,
    Moved = 3,
    Cancelled = 4,
    Delivered = 5
}
