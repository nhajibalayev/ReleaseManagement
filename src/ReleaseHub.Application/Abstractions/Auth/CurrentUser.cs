namespace ReleaseHub.Application.Abstractions.Auth;

public sealed class CurrentUser
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
