using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.Identity;

public sealed record ActiveDirectoryIdentity(
    string UserName,
    string DomainQualifiedName,
    string DisplayName,
    string? Email,
    string ExternalId);

public interface IActiveDirectoryAuthenticator
{
    ActiveDirectoryIdentity? Authenticate(string userName, string password);
}

[SupportedOSPlatform("windows")]
public sealed class ActiveDirectoryAuthenticator : IActiveDirectoryAuthenticator
{
    private readonly WindowsAuthOptions _options;
    private readonly ILogger<ActiveDirectoryAuthenticator> _logger;

    public ActiveDirectoryAuthenticator(
        IOptions<WindowsAuthOptions> options,
        ILogger<ActiveDirectoryAuthenticator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ActiveDirectoryIdentity? Authenticate(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var (domain, account) = SplitUserName(userName.Trim(), _options.Domain);

        try
        {
            using var context = string.IsNullOrWhiteSpace(domain)
                ? new PrincipalContext(ContextType.Domain)
                : new PrincipalContext(ContextType.Domain, domain);

            var valid = context.ValidateCredentials(account, password, ContextOptions.Negotiate);
            if (!valid)
            {
                // Some environments accept simple bind better with UPN.
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    valid = context.ValidateCredentials(
                        $"{account}@{domain}",
                        password,
                        ContextOptions.SimpleBind);
                }
            }

            if (!valid)
            {
                return null;
            }

            using var userPrincipal = UserPrincipal.FindByIdentity(
                context,
                IdentityType.SamAccountName,
                account)
                ?? UserPrincipal.FindByIdentity(context, userName);

            var domainName = domain;
            if (string.IsNullOrWhiteSpace(domainName))
            {
                domainName = context.Name;
            }

            var qualified = string.IsNullOrWhiteSpace(domainName)
                ? account
                : $"{domainName}\\{account}";

            var email = userPrincipal?.EmailAddress;
            var displayName = userPrincipal?.DisplayName
                ?? userPrincipal?.Name
                ?? qualified;
            var sid = userPrincipal?.Sid?.Value ?? qualified;

            return new ActiveDirectoryIdentity(
                UserName: account,
                DomainQualifiedName: qualified,
                DisplayName: displayName,
                Email: email,
                ExternalId: $"ad:{sid}");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Active Directory authentication failed for {UserName}", userName);
            return null;
        }
    }

    private static (string? Domain, string Account) SplitUserName(string userName, string defaultDomain)
    {
        if (userName.Contains('\\', StringComparison.Ordinal))
        {
            var parts = userName.Split('\\', 2);
            return (parts[0], parts[1]);
        }

        if (userName.Contains('@', StringComparison.Ordinal))
        {
            var parts = userName.Split('@', 2);
            return (parts[1], parts[0]);
        }

        return (string.IsNullOrWhiteSpace(defaultDomain) ? null : defaultDomain, userName);
    }
}
