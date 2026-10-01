using Microsoft.Extensions.Configuration.Memory;

namespace ReleaseManagement.Web.Secrets;

/// <summary>
/// Reads <see cref="SecretKeys"/> from the final <c>IConfiguration</c> (appsettings → env vars → Vault),
/// validates that everything required for the active mode is present, logs where each value came from
/// (never the value itself) and registers <see cref="AppSecrets"/>.
/// </summary>
public static class SecretsLoader
{
    public static AppSecrets AddAppSecrets(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var secrets = Load(configuration);

        foreach (var (key, origin) in secrets.Origins)
        {
            Serilog.Log.Information("Config {Key} <- {Origin}", key, origin);
        }

        var missing = MissingRequired(configuration, secrets);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required configuration (set it in Vault / ConfigMap / .env): " +
                string.Join(", ", missing.Select(key => $"{key.ConfigKey} (env {key.EnvName})")));
        }

        builder.Services.AddSingleton(secrets);
        return secrets;
    }

    public static AppSecrets Load(IConfiguration configuration)
    {
        var origins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in SecretKeys.All)
        {
            origins[key.ConfigKey] = OriginOf(configuration, key.ConfigKey);
        }

        return new AppSecrets
        {
            DbConnectionString = configuration[SecretKeys.DbConnectionString.ConfigKey] ?? string.Empty,
            AdEnabled = configuration.GetValue<bool>(SecretKeys.AdEnabled.ConfigKey),
            AdIsMock = string.Equals(configuration[SecretKeys.AdMode.ConfigKey], "Mock", StringComparison.OrdinalIgnoreCase),
            AdDomain = configuration[SecretKeys.AdDomain.ConfigKey] ?? string.Empty,
            AdLdapServer = configuration[SecretKeys.AdLdapServer.ConfigKey] ?? string.Empty,
            AdoEnabled = configuration.GetValue<bool>(SecretKeys.AdoEnabled.ConfigKey),
            AdoOrganizationUrl = configuration[SecretKeys.AdoOrganizationUrl.ConfigKey] ?? string.Empty,
            AdoProject = configuration[SecretKeys.AdoProject.ConfigKey] ?? string.Empty,
            AdoPersonalAccessToken = configuration[SecretKeys.AdoPersonalAccessToken.ConfigKey] ?? string.Empty,
            Origins = origins
        };
    }

    /// <summary>
    /// Keys that are required for the currently enabled features and are empty.
    /// DemoMode (in-memory DB) needs nothing; Mock AD / Mock ADO need no server addresses.
    /// </summary>
    public static IReadOnlyList<SecretKey> MissingRequired(IConfiguration configuration, AppSecrets secrets)
    {
        var missing = new List<SecretKey>();
        var demoMode = configuration.GetValue<bool>("DemoMode");

        if (!demoMode && string.IsNullOrWhiteSpace(secrets.DbConnectionString))
        {
            missing.Add(SecretKeys.DbConnectionString);
        }

        if (secrets.AdEnabled && !secrets.AdIsMock && !OperatingSystem.IsWindows())
        {
            // Linux containers authenticate over LDAP and need a host to talk to.
            if (string.IsNullOrWhiteSpace(secrets.AdLdapServer) && string.IsNullOrWhiteSpace(secrets.AdDomain))
            {
                missing.Add(SecretKeys.AdLdapServer);
            }
        }

        var adoIsMock = string.Equals(configuration["AzureDevOps:Mode"], "Mock", StringComparison.OrdinalIgnoreCase);
        if (secrets.AdoEnabled && !adoIsMock)
        {
            if (string.IsNullOrWhiteSpace(secrets.AdoOrganizationUrl))
            {
                missing.Add(SecretKeys.AdoOrganizationUrl);
            }

            if (string.IsNullOrWhiteSpace(secrets.AdoProject))
            {
                missing.Add(SecretKeys.AdoProject);
            }
        }

        return missing;
    }

    /// <summary>Name of the last configuration provider that has a value for the key ("-" if none).</summary>
    private static string OriginOf(IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot root)
        {
            return "?";
        }

        string origin = "-";
        foreach (var provider in root.Providers)
        {
            if (provider.TryGet(key, out var value) && !string.IsNullOrEmpty(value))
            {
                origin = provider switch
                {
                    MemoryConfigurationProvider => "memory",
                    _ => provider.ToString() ?? provider.GetType().Name
                };
            }
        }

        return origin;
    }
}
