using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public sealed class BackgroundJobSettings : IBackgroundJobSettings
{
    public BackgroundJobSettings(IOptions<HangfireOptions> options)
    {
        HangfireEnabled = options.Value.Enabled;
    }

    public bool HangfireEnabled { get; }
}

public sealed class AzureDevOpsReleaseSyncService : IAzureDevOpsReleaseSyncService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAzureDevOpsService _azureDevOps;
    private readonly IAzureDevOpsTokenProvider _tokenProvider;
    private readonly AzureDevOpsOptions _options;
    private readonly IClock _clock;

    public AzureDevOpsReleaseSyncService(
        IApplicationDbContext dbContext,
        IAzureDevOpsService azureDevOps,
        IAzureDevOpsTokenProvider tokenProvider,
        IOptions<AzureDevOpsOptions> options,
        IClock clock)
    {
        _dbContext = dbContext;
        _azureDevOps = azureDevOps;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _clock = clock;
    }

    public async Task CreateWorkItemIfNeededAsync(
        Release release,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.CreateWorkItemOnSubmit || release.AzureDevOpsWorkItemId.HasValue)
        {
            return;
        }

        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var result = await _azureDevOps.CreateReleaseWorkItemAsync(release, accessToken, cancellationToken);
        release.SetAzureDevOpsWorkItem(result.WorkItemId, result.WorkItemUrl, _clock.UtcNow);

        var mapping = await _dbContext.AzureDevOpsMappings
            .SingleOrDefaultAsync(item => item.ReleaseId == release.Id, cancellationToken);

        if (mapping is null)
        {
            mapping = new AzureDevOpsMapping(
                Guid.NewGuid(),
                release.Id,
                _options.OrganizationUrl,
                _options.Project,
                result.WorkItemId,
                result.WorkItemUrl);
            _dbContext.AzureDevOpsMappings.Add(mapping);
        }

        mapping.RecordSynchronizationSuccess(_clock.UtcNow);
    }

    public async Task UpdateWorkItemIfNeededAsync(
        Release release,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !release.AzureDevOpsWorkItemId.HasValue)
        {
            return;
        }

        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _azureDevOps.UpdateReleaseWorkItemAsync(release, accessToken, cancellationToken);
    }

    public async Task AddCommentIfNeededAsync(
        Release release,
        string comment,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled ||
            !release.AzureDevOpsWorkItemId.HasValue ||
            string.IsNullOrWhiteSpace(comment))
        {
            return;
        }

        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _azureDevOps.AddCommentAsync(
            release.AzureDevOpsWorkItemId.Value,
            comment,
            accessToken,
            cancellationToken);
    }
}
