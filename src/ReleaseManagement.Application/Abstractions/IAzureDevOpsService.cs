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
        CancellationToken cancellationToken = default);

    Task UpdateReleaseWorkItemAsync(
        Release release,
        CancellationToken cancellationToken = default);

    Task AddCommentAsync(
        int workItemId,
        string comment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AzureDevOpsWorkItemDto>> GetLinkedWorkItemsAsync(
        IEnumerable<int> workItemIds,
        CancellationToken cancellationToken = default);
}
