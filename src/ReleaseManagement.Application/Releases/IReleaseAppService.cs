using ReleaseManagement.Application.DTOs.Releases;

namespace ReleaseManagement.Application.Releases;

public interface IReleaseAppService
{
    Task<Guid> CreateDraftAsync(
        CreateReleaseDraftRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateDraftAsync(
        UpdateReleaseDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<ReleaseDetailsDto> GetByIdAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);

    Task<ReleaseStatusSummaryDto> GetStatusSummaryAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReleaseListItemDto>> GetMyReleasesAsync(
        CancellationToken cancellationToken = default);
}
