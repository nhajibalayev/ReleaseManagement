using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public sealed class AzureDevOpsProjectAccessService : IAzureDevOpsProjectAccessService
{
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsOptions _options;
    private readonly IAzureDevOpsTokenProvider _tokenProvider;
    private readonly ILogger<AzureDevOpsProjectAccessService> _logger;

    public AzureDevOpsProjectAccessService(
        HttpClient httpClient,
        IOptions<AzureDevOpsOptions> options,
        IAzureDevOpsTokenProvider tokenProvider,
        ILogger<AzureDevOpsProjectAccessService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public bool IsEnforced =>
        _options.Enabled &&
        _options.RequireProjectAccessToCreate;

    public async Task EnsureCurrentUserCanAccessProjectAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsEnforced)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.OrganizationUrl) ||
            string.IsNullOrWhiteSpace(_options.Project))
        {
            throw new InvalidOperationException("Azure DevOps project settings are incomplete.");
        }

        var url =
            $"{_options.OrganizationUrl.TrimEnd('/')}/_apis/projects/{Uri.EscapeDataString(_options.Project)}?api-version={_options.ApiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var oauthToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(oauthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
        }
        else if (!string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            var basic = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }
        else if (!_options.UseWindowsCredentials)
        {
            throw new ForbiddenException(
                "Azure DevOps credentials are not configured. Provide a PAT or enable Windows credentials.");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        _logger.LogWarning(
            "Azure DevOps project access denied. Status={StatusCode}, Project={Project}",
            (int)response.StatusCode,
            _options.Project);

        throw new ForbiddenException(
            $"You do not have access to Azure DevOps project '{_options.Project}'. " +
            "Only users with board access can create releases.");
    }
}
