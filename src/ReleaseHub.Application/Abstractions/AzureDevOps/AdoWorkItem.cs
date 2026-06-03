namespace ReleaseHub.Application.Abstractions.AzureDevOps;

public sealed class AdoWorkItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public sealed class CreateWorkItemRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string CreatedByEmail { get; init; }
    public Guid ExternalId { get; init; }
}
