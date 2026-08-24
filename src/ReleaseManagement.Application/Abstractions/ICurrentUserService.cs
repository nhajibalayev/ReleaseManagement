namespace ReleaseManagement.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }

    string UserName { get; }

    string Email { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
