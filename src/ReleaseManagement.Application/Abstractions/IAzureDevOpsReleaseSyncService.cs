using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Application.Abstractions;

public interface IBackgroundJobSettings
{
    bool HangfireEnabled { get; }
}

public interface IAzureDevOpsReleaseSyncService
{
    Task CreateWorkItemIfNeededAsync(Release release, CancellationToken cancellationToken = default);

    Task UpdateWorkItemIfNeededAsync(Release release, CancellationToken cancellationToken = default);

    Task AddCommentIfNeededAsync(
        Release release,
        string comment,
        CancellationToken cancellationToken = default);
}
