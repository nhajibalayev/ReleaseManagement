using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Application.Abstractions;

public sealed record AzureDevOpsWorkItemResult(int WorkItemId, string WorkItemUrl);

public sealed record AzureDevOpsWorkItemDto(
    int WorkItemId,
    string WorkItemType,
    string Title,
    string WorkItemUrl,
    string? State);

public interface IAzureDevOpsService
{
    Task<AzureDevOpsWorkItemResult> CreateReleaseWorkItemAsync(
        Release release,
        string? accessToken = null,
        CancellationToken cancellationToken = default);

    Task UpdateReleaseWorkItemAsync(
        Release release,
        string? accessToken = null,
        CancellationToken cancellationToken = default);

    Task AddCommentAsync(
        int workItemId,
        string comment,
        string? accessToken = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AzureDevOpsWorkItemDto>> GetLinkedWorkItemsAsync(
        IEnumerable<int> workItemIds,
        string? accessToken = null,
        CancellationToken cancellationToken = default);
}
