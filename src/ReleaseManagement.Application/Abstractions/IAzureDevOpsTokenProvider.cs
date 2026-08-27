namespace ReleaseManagement.Application.Abstractions;

/// <summary>
/// Resolves Azure DevOps authorization for the current caller.
/// Returns a full Authorization header value such as "Basic …" or "Bearer …".
/// Prefers the signed-in AD user's credentials; then Entra OAuth when enabled.
/// </summary>
public interface IAzureDevOpsTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
