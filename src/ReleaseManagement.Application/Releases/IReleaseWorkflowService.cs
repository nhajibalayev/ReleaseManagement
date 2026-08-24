using ReleaseManagement.Application.DTOs.Releases;
using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Releases;

public interface IReleaseWorkflowService
{
    bool CanTransition(
        ReleaseStatus currentStatus,
        ReleaseStatus targetStatus,
        IReadOnlyCollection<string> userRoles);

    Task TransitionAsync(
        Guid releaseId,
        ReleaseStatus targetStatus,
        string? comment,
        CancellationToken cancellationToken = default);

    Task TransitionAsync(
        TransitionReleaseRequest request,
        CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid releaseId, CancellationToken cancellationToken = default);
}
