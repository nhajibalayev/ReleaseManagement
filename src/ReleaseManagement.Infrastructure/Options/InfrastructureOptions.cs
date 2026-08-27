namespace ReleaseManagement.Infrastructure.Options;

public sealed class WindowsAuthOptions
{
    public const string SectionName = "WindowsAuth";

    /// <summary>
    /// When true, users sign in with Active Directory username/password.
    /// Those credentials are used dynamically for Azure DevOps Server API calls.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Optional AD domain (e.g. NH-NK). If empty, the machine's domain is used.
    /// Users may still type DOMAIN\user or user@domain.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// When true, also offer browser Negotiate (integrated Windows) login.
    /// </summary>
    public bool EnableNegotiate { get; set; }

    /// <summary>Keep local Identity seed accounts available (dev only).</summary>
    public bool AllowLocalLogin { get; set; } = true;

    /// <summary>Default app role for newly provisioned AD users.</summary>
    public string DefaultRole { get; set; } = "ProductOwner";
}

public sealed class AzureAdOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>When true, Microsoft Entra ID SSO is offered on the login page.</summary>
    public bool Enabled { get; set; }

    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>Keep username/password login available alongside SSO.</summary>
    public bool AllowLocalLogin { get; set; } = true;

    /// <summary>Default app role assigned to newly provisioned SSO users.</summary>
    public string DefaultRole { get; set; } = "ProductOwner";
}

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    /// <summary>
    /// Collection/organization base URL.
    /// On-prem example: https://devops.nh-nk.az/DefaultCollection
    /// Cloud example: https://dev.azure.com/contoso
    /// </summary>
    public string OrganizationUrl { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// Optional fallback only. Preferred path is the signed-in user's AD credentials.
    /// </summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    public string WorkItemType { get; set; } = "Task";

    public bool Enabled { get; set; }

    /// <summary>REST API version. Prefer 7.1; older servers may need 6.0 or 5.1.</summary>
    public string ApiVersion { get; set; } = "7.1";

    /// <summary>
    /// Optional Area Path so the work item appears on the team/release board
    /// (e.g. "MyProject\\Release Team").
    /// </summary>
    public string AreaPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional Iteration Path (e.g. "MyProject\\Release\\Sprint 1").
    /// </summary>
    public string IterationPath { get; set; } = string.Empty;

    /// <summary>
    /// When true, HttpClient sends Windows credentials (Negotiate path / process identity).
    /// </summary>
    public bool UseWindowsCredentials { get; set; }

    /// <summary>
    /// Azure DevOps Services OAuth scope (cloud / Entra only).
    /// </summary>
    public string OAuthScope { get; set; } =
        "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation";

    /// <summary>
    /// When true, create-release verifies the configured ADO project is reachable
    /// with the signed-in user's credentials.
    /// </summary>
    public bool RequireProjectAccessToCreate { get; set; } = true;
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 25;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "noreply@releasemanagement.local";

    public bool Enabled { get; set; }
}

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "App_Data/attachments";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public string DefaultPassword { get; set; } = "ChangeMe!123";
}
