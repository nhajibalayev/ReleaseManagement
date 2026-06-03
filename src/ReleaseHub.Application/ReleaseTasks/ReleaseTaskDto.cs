using ReleaseHub.Domain.Enums;

namespace ReleaseHub.Application.ReleaseTasks;

public sealed class ReleaseTaskDto
{
    public Guid Id { get; init; }
    public int? AdoWorkItemId { get; init; }
    public string Title { get; init; } = string.Empty;
    public ReleaseTaskState State { get; init; }
    public string CreatedByEmail { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
