namespace ReleaseManagement.Domain.Constants;

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string ProductOwner = "ProductOwner";
    public const string ReleaseManager = "ReleaseManager";
    public const string Pentest = "Pentest";
    public const string InfoSec = "InfoSec";
    public const string BusinessApprover = "BusinessApprover";
    public const string DevOps = "DevOps";
    public const string Auditor = "Auditor";
    public const string QA = "QA";
    public const string Risk = "Risk";
    public const string ChapterLead = "ChapterLead";

    // Procedure v4.0 §2 roles.
    public const string TechnicalOwner = "TechnicalOwner";
    public const string DBA = "DBA";
    public const string ITOperations = "ITOperations";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Administrator,
            ProductOwner,
            ReleaseManager,
            Pentest,
            InfoSec,
            BusinessApprover,
            DevOps,
            Auditor,
            QA,
            Risk,
            ChapterLead,
            TechnicalOwner,
            DBA,
            ITOperations
        };
}
