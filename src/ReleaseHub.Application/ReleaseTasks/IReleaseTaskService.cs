namespace ReleaseHub.Application.ReleaseTasks;

public interface IReleaseTaskService
{
    Task<IReadOnlyList<ReleaseTaskDto>> ListAsync(CancellationToken ct = default);
    Task<ReleaseTaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReleaseTaskDto> CreateAsync(CreateReleaseTaskRequest request, CancellationToken ct = default);
}
