#if CORP_VAULT
using PL.Common.SecretVault;
#endif

namespace ReleaseManagement.Web.Secrets;

/// <summary>
/// HashiCorp Vault hook. The corporate package <c>PL.Common.SecretVault</c> is only available from the
/// internal NuGet mirror, so it is referenced conditionally: build with <c>-p:CorpVault=true</c>
/// (Dockerfile: <c>--build-arg CORP_VAULT=true</c>) to compile this in. Without it the method is a no-op
/// and secrets come from appsettings / environment variables / .env, which is what local runs use.
/// </summary>
public static class CorporateVault
{
    public static WebApplicationBuilder AddCorporateVault(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            Serilog.Log.Information("Vault: skipped in Development (secrets come from env / user-secrets)");
            return builder;
        }

#if CORP_VAULT
        // Reads Vault address / Kubernetes role / secret path from configuration (section "Vault")
        // and loads the secrets so that SecretsLoader can validate them afterwards.
        builder.Services.AddVaultWithKubernetes(builder.Configuration);
        Serilog.Log.Information("Vault: PL.Common.SecretVault (Kubernetes auth) registered");
#else
        Serilog.Log.Warning(
            "Vault: not compiled in (build with -p:CorpVault=true); using appsettings / environment variables only");
#endif
        return builder;
    }
}
