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
            Auditor
        };
}
