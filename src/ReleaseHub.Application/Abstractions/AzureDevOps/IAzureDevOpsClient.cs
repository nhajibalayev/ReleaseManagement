namespace ReleaseHub.Application.Abstractions.AzureDevOps;

public interface IAzureDevOpsClient
{
    Task<AdoWorkItem> CreateReleaseTaskAsync(CreateWorkItemRequest request, CancellationToken ct = default);
    Task<AdoWorkItem?> GetAsync(int workItemId, CancellationToken ct = default);
    Task UpdateStateAsync(int workItemId, string state, CancellationToken ct = default);
    Task AddCommentAsync(int workItemId, string commentHtml, CancellationToken ct = default);
}
