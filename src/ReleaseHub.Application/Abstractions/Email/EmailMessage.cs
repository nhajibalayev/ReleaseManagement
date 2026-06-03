namespace ReleaseHub.Application.Abstractions.Email;

public sealed class EmailMessage
{
    public required IReadOnlyCollection<string> To { get; init; }
    public IReadOnlyCollection<string> Cc { get; init; } = Array.Empty<string>();
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public bool IsHtml { get; init; } = true;
}
