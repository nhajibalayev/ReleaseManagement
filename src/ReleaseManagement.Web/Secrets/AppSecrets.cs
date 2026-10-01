namespace ReleaseManagement.Web.Secrets;

/// <summary>
/// Typed, validated snapshot of the values listed in <see cref="SecretKeys"/>.
/// Registered as a singleton by <see cref="SecretsLoader"/>; inject it wherever a secret is needed
/// instead of reading <c>IConfiguration</c> ad hoc.
/// </summary>
public sealed class AppSecrets
{
    public required string DbConnectionString { get; init; }

    public required bool AdEnabled { get; init; }

    public required bool AdIsMock { get; init; }

    public required string AdDomain { get; init; }

    public required string AdLdapServer { get; init; }

    public required bool AdoEnabled { get; init; }

    public required string AdoOrganizationUrl { get; init; }

    public required string AdoProject { get; init; }

    public required string AdoPersonalAccessToken { get; init; }

    /// <summary>Which configuration provider supplied each key — for the startup log and troubleshooting.</summary>
    public required IReadOnlyDictionary<string, string> Origins { get; init; }
}
