using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public sealed class AzureDevOpsTokenProvider : IAzureDevOpsTokenProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureDevOpsUserCredentialStore _credentialStore;
    private readonly AzureDevOpsOptions _options;
    private readonly AzureAdOptions _azureAd;
    private readonly ILogger<AzureDevOpsTokenProvider> _logger;

    public AzureDevOpsTokenProvider(
        IServiceProvider serviceProvider,
        IAzureDevOpsUserCredentialStore credentialStore,
        IOptions<AzureDevOpsOptions> options,
        IOptions<AzureAdOptions> azureAd,
        ILogger<AzureDevOpsTokenProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _credentialStore = credentialStore;
        _options = options.Value;
        _azureAd = azureAd.Value;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Prefer the signed-in AD user's credentials (Basic for on-prem DevOps Server).
        var credential = _credentialStore.Get();
        if (credential is not null)
        {
            return credential.ToAuthorizationValue();
        }

        if (_azureAd.Enabled)
        {
            var tokenAcquisition = _serviceProvider.GetService<ITokenAcquisition>();
            if (tokenAcquisition is not null)
            {
                try
                {
                    var bearer = await tokenAcquisition.GetAccessTokenForUserAsync(
                        [_options.OAuthScope]);
                    if (!string.IsNullOrWhiteSpace(bearer))
                    {
                        return $"Bearer {bearer}";
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to acquire Azure DevOps OAuth token for the signed-in user.");
                }
            }
        }

        return null;
    }
}
