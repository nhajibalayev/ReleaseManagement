using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.AzureDevOps;
using ReleaseHub.Application.Options;

namespace ReleaseHub.Infrastructure.AzureDevOps;

public sealed class AzureDevOpsClient : IAzureDevOpsClient
{
    public const string HttpClientName = "AzureDevOps";
    private const string ApiVersion = "7.1";

    private readonly HttpClient _http;
    private readonly AzureDevOpsOptions _options;
    private readonly ILogger<AzureDevOpsClient> _log;

    public AzureDevOpsClient(HttpClient http, IOptions<AzureDevOpsOptions> options, ILogger<AzureDevOpsClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<AdoWorkItem> CreateReleaseTaskAsync(CreateWorkItemRequest request, CancellationToken ct = default)
    {
        var path = $"{Uri.EscapeDataString(_options.Project)}/_apis/wit/workitems/${Uri.EscapeDataString(_options.WorkItemType)}?api-version={ApiVersion}";
        var ops = new[]
        {
            new { op = "add", path = "/fields/System.Title", value = request.Title },
            new { op = "add", path = "/fields/System.Description", value = request.Description },
            new { op = "add", path = "/fields/System.Tags", value = $"ReleaseHub;ext:{request.ExternalId}" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(ops, mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json"))
        };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty work item response.");
        return Map(payload);
    }

    public async Task<AdoWorkItem?> GetAsync(int workItemId, CancellationToken ct = default)
    {
        var path = $"_apis/wit/workitems/{workItemId}?api-version={ApiVersion}";
        using var resp = await _http.GetAsync(path, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var payload = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(cancellationToken: ct);
        return payload is null ? null : Map(payload);
    }

    public async Task UpdateStateAsync(int workItemId, string state, CancellationToken ct = default)
    {
        var path = $"_apis/wit/workitems/{workItemId}?api-version={ApiVersion}";
        var ops = new[] { new { op = "add", path = "/fields/System.State", value = state } };
        using var req = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(ops, mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json"))
        };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task AddCommentAsync(int workItemId, string commentHtml, CancellationToken ct = default)
    {
        var path = $"{Uri.EscapeDataString(_options.Project)}/_apis/wit/workItems/{workItemId}/comments?api-version={ApiVersion}-preview.3";
        using var resp = await _http.PostAsJsonAsync(path, new { text = commentHtml }, ct);
        resp.EnsureSuccessStatusCode();
    }

    private static AdoWorkItem Map(WorkItemResponse w) => new()
    {
        Id = w.Id,
        Title = w.Fields?.Title ?? string.Empty,
        State = w.Fields?.State ?? string.Empty,
        Url = w.Url ?? string.Empty
    };

    private sealed class WorkItemResponse
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("fields")] public WorkItemFields? Fields { get; set; }
    }

    private sealed class WorkItemFields
    {
        [JsonPropertyName("System.Title")] public string? Title { get; set; }
        [JsonPropertyName("System.State")] public string? State { get; set; }
    }
}
