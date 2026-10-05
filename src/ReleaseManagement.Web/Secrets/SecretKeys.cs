namespace ReleaseManagement.Web.Secrets;

/// <summary>
/// Where a configuration value is expected to come from in production.
/// </summary>
public enum SecretSource
{
    /// <summary>Sensitive value: lives only in HashiCorp Vault.</summary>
    Vault,

    /// <summary>Non-sensitive value: Kubernetes ConfigMap / environment variable.</summary>
    ConfigMap
}

/// <summary>
/// One configuration key the application depends on.
/// </summary>
/// <param name="ConfigKey">Key as the application reads it from <c>IConfiguration</c> (colon-separated).</param>
/// <param name="Description">What the value is, for the person who fills it in.</param>
/// <param name="Source">Vault (secret) or ConfigMap (plain setting).</param>
/// <param name="Required">When true, startup fails if the key is empty (see <see cref="SecretsLoader"/>).</param>
public sealed record SecretKey(string ConfigKey, string Description, SecretSource Source, bool Required)
{
    /// <summary>
    /// The same key in environment-variable form (<c>Section__Key</c>).
    /// This is the name to create in Vault / ConfigMap when the secrets are delivered as env vars.
    /// </summary>
    public string EnvName => ConfigKey.Replace(":", "__", StringComparison.Ordinal);
}

/// <summary>
/// THE single list of every secret / setting the platform needs from outside.
/// Add a new key here first; then use <see cref="AppSecrets"/> to read it.
/// <c>docs/CONFIGURATION.md</c> is generated from this list by hand — keep them in sync.
/// </summary>
public static class SecretKeys
{
    // ---------- Database ----------
    public static readonly SecretKey DbConnectionString = new(
        "ConnectionStrings:DefaultConnection",
        "PostgreSQL connection string: Host=...;Port=5432;Database=...;Username=...;Password=...",
        SecretSource.Vault,
        Required: true);

    // ---------- Active Directory login ----------
    public static readonly SecretKey AdEnabled = new(
        "WindowsAuth:Enabled",
        "true = users sign in with AD user name + password.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdMode = new(
        "WindowsAuth:Mode",
        "Real (validate against the domain) or Mock (any user with WindowsAuth:MockPassword).",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdDomain = new(
        "WindowsAuth:Domain",
        "AD DNS domain, e.g. test-namiq.local (echo %USERDNSDOMAIN%).",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdLdapServer = new(
        "WindowsAuth:LdapServer",
        "Domain controller FQDN for LDAP bind in Linux containers, e.g. mcpads01.test-namiq.local (echo %LOGONSERVER%).",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdNetbiosDomain = new(
        "WindowsAuth:NetbiosDomain",
        "Short (NetBIOS) domain for NTLM to Azure DevOps Server, e.g. TEST-NAMIQ (echo %USERDOMAIN%).",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdLdapUseSsl = new(
        "WindowsAuth:LdapUseSsl",
        "true = LDAPS (636). Recommended; plain LDAP sends the password in clear text.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdDefaultRole = new(
        "WindowsAuth:DefaultRole",
        "Role given to a user on first AD sign-in (usually ProductOwner).",
        SecretSource.ConfigMap,
        Required: false);

    // ---------- Azure DevOps Server ----------
    public static readonly SecretKey AdoEnabled = new(
        "AzureDevOps:Enabled",
        "true = integrate with Azure DevOps Server.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdoOrganizationUrl = new(
        "AzureDevOps:OrganizationUrl",
        "Collection URL, e.g. https://devops.jora-life.com/DefaultCollection.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdoProject = new(
        "AzureDevOps:Project",
        "Project name with real spaces, e.g. Release Management Board.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdoWorkItemType = new(
        "AzureDevOps:WorkItemType",
        "Work item type to create (Task / Release).",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdoApiVersion = new(
        "AzureDevOps:ApiVersion",
        "REST API version; 6.0 is known to work on-prem.",
        SecretSource.ConfigMap,
        Required: false);

    public static readonly SecretKey AdoPersonalAccessToken = new(
        "AzureDevOps:PersonalAccessToken",
        "Optional fallback PAT. Normally EMPTY: the signed-in user's AD credentials are used (NTLM).",
        SecretSource.Vault,
        Required: false);

    // ---------- Bootstrap / startup ----------
    public static readonly SecretKey AdminPassword = new(
        "Seed:AdminPassword",
        "Password of the built-in local 'admin' account created on first start (Seed:BootstrapAdmin). Change after first login.",
        SecretSource.Vault,
        Required: false);

    public static readonly SecretKey ApplyMigrations = new(
        "Seed:ApplyMigrations",
        "true = apply EF migrations at startup (single replica); false = run migrations as a separate job.",
        SecretSource.ConfigMap,
        Required: false);

    // ---------- Email ----------
    public static readonly SecretKey EmailPassword = new(
        "Email:Password",
        "SMTP password (only when Email:Enabled = true).",
        SecretSource.Vault,
        Required: false);

    /// <summary>Every key, in the order shown in docs/CONFIGURATION.md.</summary>
    public static IReadOnlyList<SecretKey> All { get; } =
    [
        DbConnectionString,
        AdEnabled,
        AdMode,
        AdDomain,
        AdLdapServer,
        AdNetbiosDomain,
        AdLdapUseSsl,
        AdDefaultRole,
        AdoEnabled,
        AdoOrganizationUrl,
        AdoProject,
        AdoWorkItemType,
        AdoApiVersion,
        AdoPersonalAccessToken,
        AdminPassword,
        ApplyMigrations,
        EmailPassword
    ];
}
