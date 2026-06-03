namespace ReleaseHub.Application.Abstractions.Auth;

public interface ISsoClient
{
    Task<CurrentUser?> ValidateAsync(string accessToken, CancellationToken ct = default);
}
