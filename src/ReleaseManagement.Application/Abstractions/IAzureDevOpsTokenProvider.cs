namespace ReleaseManagement.Application.Abstractions;

/// <summary>
/// Resolves an Azure DevOps access token for the current caller.
/// Prefer the signed-in user's OAuth token; fall back to configured PAT when allowed.
/// </summary>
public interface IAzureDevOpsTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
