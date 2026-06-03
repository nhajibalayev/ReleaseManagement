namespace ReleaseHub.Application.ReleaseTasks;

public interface IReleaseTaskService
{
    Task<IReadOnlyList<ReleaseTaskDto>> ListAsync(CancellationToken ct = default);
}
