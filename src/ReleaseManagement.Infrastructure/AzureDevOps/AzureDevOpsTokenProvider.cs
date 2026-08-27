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
    private readonly AzureDevOpsOptions _options;
    private readonly AzureAdOptions _azureAd;
    private readonly ILogger<AzureDevOpsTokenProvider> _logger;

    public AzureDevOpsTokenProvider(
        IServiceProvider serviceProvider,
        IOptions<AzureDevOpsOptions> options,
        IOptions<AzureAdOptions> azureAd,
        ILogger<AzureDevOpsTokenProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _azureAd = azureAd.Value;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_azureAd.Enabled)
        {
            var tokenAcquisition = _serviceProvider.GetService<ITokenAcquisition>();
            if (tokenAcquisition is not null)
            {
                try
                {
                    return await tokenAcquisition.GetAccessTokenForUserAsync(
                        [_options.OAuthScope]);
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
