namespace ReleaseHub.Application.Abstractions.Auth;

public interface ICurrentUserAccessor
{
    CurrentUser? User { get; }
    bool IsAuthenticated { get; }
}
