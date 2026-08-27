namespace ReleaseManagement.Application.Abstractions;

public interface IAzureDevOpsProjectAccessService
{
    /// <summary>
    /// Returns true when Azure DevOps project/board access checks are enforced.
    /// </summary>
    bool IsEnforced { get; }

    Task EnsureCurrentUserCanAccessProjectAsync(CancellationToken cancellationToken = default);
}
