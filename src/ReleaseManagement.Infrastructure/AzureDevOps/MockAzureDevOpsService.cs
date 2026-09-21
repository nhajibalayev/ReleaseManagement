using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

/// <summary>
/// In-memory stand-in for Azure DevOps Server used on machines without corporate network access
/// (<c>AzureDevOps:Mode = "Mock"</c>). Work items live for the lifetime of the process.
/// </summary>
public sealed class MockAzureDevOpsService : IAzureDevOpsService
{
    private static readonly ConcurrentDictionary<int, MockWorkItem> WorkItems = new();
    private static int _nextId = 10_000;

    private readonly AzureDevOpsOptions _options;
    private readonly ILogger<MockAzureDevOpsService> _logger;

    public MockAzureDevOpsService(
        IOptions<AzureDevOpsOptions> options,
        ILogger<MockAzureDevOpsService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AzureDevOpsWorkItemResult> CreateReleaseWorkItemAsync(
        Release release,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        var id = Interlocked.Increment(ref _nextId);
        var url = $"{BaseUrl()}/_workitems/edit/{id}";
        WorkItems[id] = new MockWorkItem(id, _options.WorkItemType, $"{release.ReleaseNumber}: {release.Title}", url, "New");

        _logger.LogInformation("[MockADO] Created work item {WorkItemId} for release {ReleaseNumber}", id, release.ReleaseNumber);
        return Task.FromResult(new AzureDevOpsWorkItemResult(id, url));
    }

    public Task UpdateReleaseWorkItemAsync(
        Release release,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        if (release.AzureDevOpsWorkItemId is { } id && WorkItems.TryGetValue(id, out var item))
        {
            WorkItems[id] = item with { State = release.CurrentStatus.ToString() };
            _logger.LogInformation("[MockADO] Updated work item {WorkItemId} → {State}", id, release.CurrentStatus);
        }

        return Task.CompletedTask;
    }

    public Task AddCommentAsync(
        int workItemId,
        string comment,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MockADO] Discussion comment on {WorkItemId}: {Comment}", workItemId, comment);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AzureDevOpsWorkItemDto>> GetLinkedWorkItemsAsync(
        IEnumerable<int> workItemIds,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItemIds);

        var results = workItemIds
            .Select(id => WorkItems.TryGetValue(id, out var item)
                ? new AzureDevOpsWorkItemDto(item.Id, item.Type, item.Title, item.Url, item.State)
                : new AzureDevOpsWorkItemDto(id, "Task", $"Mock work item {id}", $"{BaseUrl()}/_workitems/edit/{id}", "Active"))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AzureDevOpsWorkItemDto>>(results);
    }

    private string BaseUrl()
    {
        var organization = string.IsNullOrWhiteSpace(_options.OrganizationUrl)
            ? "https://mock-devops.local/DefaultCollection"
            : _options.OrganizationUrl.TrimEnd('/');
        var project = string.IsNullOrWhiteSpace(_options.Project) ? "Mock Project" : _options.Project;
        return $"{organization}/{Uri.EscapeDataString(project)}";
    }

    private sealed record MockWorkItem(int Id, string Type, string Title, string Url, string? State);
}

/// <summary>Mock project access: every signed-in user is allowed to create releases.</summary>
public sealed class MockAzureDevOpsProjectAccessService : IAzureDevOpsProjectAccessService
{
    public bool IsEnforced => false;

    public Task EnsureCurrentUserCanAccessProjectAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
