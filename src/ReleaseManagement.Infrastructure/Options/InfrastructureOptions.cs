namespace ReleaseManagement.Infrastructure.Options;

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

    public string OrganizationUrl { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    /// <summary>Optional fallback PAT when user OAuth token is unavailable (e.g. local/dev).</summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    public string WorkItemType { get; set; } = "Release";

    public bool Enabled { get; set; }

    /// <summary>
    /// Azure DevOps resource GUID scope for delegated user tokens.
    /// </summary>
    public string OAuthScope { get; set; } =
        "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation";

    /// <summary>
    /// When true (and Azure AD SSO is enabled), create-release requires ADO project access.
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
