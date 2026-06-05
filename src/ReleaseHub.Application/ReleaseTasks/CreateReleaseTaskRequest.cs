namespace ReleaseHub.Application.ReleaseTasks;

public sealed class CreateReleaseTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
