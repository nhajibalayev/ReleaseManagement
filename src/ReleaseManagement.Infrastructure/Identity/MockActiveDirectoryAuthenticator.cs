using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.Identity;

/// <summary>
/// Development stand-in for Active Directory (<c>WindowsAuth:Mode = "Mock"</c>).
/// Any user name signs in when the password equals <c>WindowsAuth:MockPassword</c>;
/// other passwords fall through to the local Identity accounts.
/// </summary>
public sealed class MockActiveDirectoryAuthenticator : IActiveDirectoryAuthenticator
{
    private static readonly char[] NameSeparators = ['.', '_', '-'];

    private readonly WindowsAuthOptions _options;
    private readonly ILogger<MockActiveDirectoryAuthenticator> _logger;

    public MockActiveDirectoryAuthenticator(
        IOptions<WindowsAuthOptions> options,
        ILogger<MockActiveDirectoryAuthenticator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ActiveDirectoryIdentity? Authenticate(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) ||
            !string.Equals(password, _options.MockPassword, StringComparison.Ordinal))
        {
            return null;
        }

        var account = userName.Trim();
        var domain = string.IsNullOrWhiteSpace(_options.Domain) ? "MOCK" : _options.Domain.Trim();

        var separator = account.IndexOf('\\', StringComparison.Ordinal);
        if (separator >= 0)
        {
            domain = account[..separator];
            account = account[(separator + 1)..];
        }

        var at = account.IndexOf('@', StringComparison.Ordinal);
        if (at >= 0)
        {
            domain = account[(at + 1)..];
            account = account[..at];
        }

        var displayName = string.Join(
            ' ',
            account.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

        _logger.LogInformation("[MockAD] Authenticated {Domain}\\{Account}", domain, account);

        return new ActiveDirectoryIdentity(
            account,
            $"{domain}\\{account}",
            string.IsNullOrWhiteSpace(displayName) ? account : displayName,
            $"{account}@{domain.ToLowerInvariant()}.local",
            $"mock-ad:{domain.ToUpperInvariant()}\\{account.ToLowerInvariant()}");
    }
}
