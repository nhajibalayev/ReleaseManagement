using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public sealed class AzureDevOpsService : IAzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsOptions _options;
    private readonly ILogger<AzureDevOpsService> _logger;

    public AzureDevOpsService(
        HttpClient httpClient,
        IOptions<AzureDevOpsOptions> options,
        ILogger<AzureDevOpsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AzureDevOpsWorkItemResult> CreateReleaseWorkItemAsync(
        Release release,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var document = new object[]
        {
            new { op = "add", path = "/fields/System.Title", value = $"{release.ReleaseNumber}: {release.Title}" },
            new { op = "add", path = "/fields/System.Description", value = release.Description },
            new { op = "add", path = "/fields/System.Tags", value = $"ReleaseManagement;{release.ReleaseNumber}" }
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(document),
            Encoding.UTF8,
            "application/json-patch+json");

        var url =
            $"{_options.OrganizationUrl.TrimEnd('/')}/{_options.Project}/_apis/wit/workitems/${_options.WorkItemType}?api-version=7.1";

        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Azure DevOps create failed. Status={StatusCode}; Body={Body}",
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"Azure DevOps create failed with status {(int)response.StatusCode}.");
        }

        using var documentJson = JsonDocument.Parse(body);
        var id = documentJson.RootElement.GetProperty("id").GetInt32();
        var workItemUrl = documentJson.RootElement.GetProperty("url").GetString()
            ?? $"{_options.OrganizationUrl.TrimEnd('/')}/{_options.Project}/_workitems/edit/{id}";

        return new AzureDevOpsWorkItemResult(id, workItemUrl);
    }

    public async Task UpdateReleaseWorkItemAsync(
        Release release,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (release.AzureDevOpsWorkItemId is null)
        {
            return;
        }

        var document = new object[]
        {
            new
            {
                op = "add",
                path = "/fields/System.History",
                value = $"Status updated to {release.CurrentStatus} by Release Management Platform."
            }
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(document),
            Encoding.UTF8,
            "application/json-patch+json");

        var url =
            $"{_options.OrganizationUrl.TrimEnd('/')}/{_options.Project}/_apis/wit/workitems/{release.AzureDevOpsWorkItemId}?api-version=7.1";

        using var response = await _httpClient.PatchAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Azure DevOps update failed with status {(int)response.StatusCode}: {body}");
        }
    }

    public async Task AddCommentAsync(
        int workItemId,
        string comment,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var document = new object[]
        {
            new { op = "add", path = "/fields/System.History", value = comment }
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(document),
            Encoding.UTF8,
            "application/json-patch+json");

        var url =
            $"{_options.OrganizationUrl.TrimEnd('/')}/{_options.Project}/_apis/wit/workitems/{workItemId}?api-version=7.1";

        using var response = await _httpClient.PatchAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<AzureDevOpsWorkItemDto>> GetLinkedWorkItemsAsync(
        IEnumerable<int> workItemIds,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var ids = workItemIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var url =
            $"{_options.OrganizationUrl.TrimEnd('/')}/{_options.Project}/_apis/wit/workitems?ids={string.Join(',', ids)}&api-version=7.1";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<AzureDevOpsWorkItemDto>();
        foreach (var item in document.RootElement.GetProperty("value").EnumerateArray())
        {
            var fields = item.GetProperty("fields");
            results.Add(new AzureDevOpsWorkItemDto(
                item.GetProperty("id").GetInt32(),
                fields.TryGetProperty("System.WorkItemType", out var type) ? type.GetString() ?? "Unknown" : "Unknown",
                fields.TryGetProperty("System.Title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("url", out var urlProperty) ? urlProperty.GetString() ?? string.Empty : string.Empty,
                fields.TryGetProperty("System.State", out var state) ? state.GetString() : null));
        }

        return results;
    }

    private void EnsureConfigured()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Azure DevOps integration is disabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.OrganizationUrl) ||
            string.IsNullOrWhiteSpace(_options.Project) ||
            string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            throw new InvalidOperationException("Azure DevOps settings are incomplete.");
        }
    }
}

public static class AzureDevOpsHttpClientConfigurator
{
    public static void Configure(HttpClient client, AzureDevOpsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OrganizationUrl))
        {
            client.BaseAddress = new Uri(options.OrganizationUrl.TrimEnd('/') + "/");
        }

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            var token = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{options.PersonalAccessToken}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", token);
        }
    }
}
