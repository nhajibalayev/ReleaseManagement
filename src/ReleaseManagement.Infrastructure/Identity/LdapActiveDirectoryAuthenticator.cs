using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.Identity;

/// <summary>
/// Cross-platform Active Directory authenticator (LDAP simple bind + user lookup).
/// Used automatically on Linux containers (Kubernetes / Docker), where
/// <see cref="ActiveDirectoryAuthenticator"/> (System.DirectoryServices.AccountManagement)
/// is not supported, or on any OS when <c>WindowsAuth:LdapServer</c> is configured.
/// Produces the same <c>ad:{SID}</c> external id as the Windows implementation so user
/// accounts stay linked when the app moves between hosts.
/// </summary>
public sealed class LdapActiveDirectoryAuthenticator : IActiveDirectoryAuthenticator
{
    private static readonly string[] UserAttributes =
    [
        "sAMAccountName",
        "displayName",
        "name",
        "mail",
        "userPrincipalName",
        "objectSid"
    ];

    private readonly WindowsAuthOptions _options;
    private readonly ILogger<LdapActiveDirectoryAuthenticator> _logger;

    public LdapActiveDirectoryAuthenticator(
        IOptions<WindowsAuthOptions> options,
        ILogger<LdapActiveDirectoryAuthenticator> logger)
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

        var server = string.IsNullOrWhiteSpace(_options.LdapServer) ? domain : _options.LdapServer.Trim();
        if (string.IsNullOrWhiteSpace(server))
        {
            _logger.LogError(
                "LDAP authentication is not configured: set WindowsAuth:LdapServer (domain controller host) or WindowsAuth:Domain.");
            return null;
        }

        var port = _options.LdapPort > 0 ? _options.LdapPort : (_options.LdapUseSsl ? 636 : 389);

        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(server, port))
            {
                AuthType = AuthType.Basic
            };

            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            if (_options.LdapUseSsl)
            {
                connection.SessionOptions.SecureSocketLayer = true;
                if (_options.LdapSkipCertificateValidation)
                {
                    connection.SessionOptions.VerifyServerCertificate = (_, _) => true;
                }
            }

            connection.Timeout = TimeSpan.FromSeconds(15);

            // AD accepts both "user@dns.domain" and "NETBIOS\user" for simple bind.
            var bindName = BuildBindName(domain, account);
            connection.Credential = new NetworkCredential(bindName, password);
            connection.Bind();

            var baseDn = string.IsNullOrWhiteSpace(_options.LdapBaseDn)
                ? ResolveDefaultNamingContext(connection)
                : _options.LdapBaseDn.Trim();

            var entry = FindUser(connection, baseDn, account);

            var domainName = string.IsNullOrWhiteSpace(domain) ? _options.Domain : domain;
            var qualified = string.IsNullOrWhiteSpace(domainName)
                ? account
                : $"{domainName}\\{account}";

            var sam = GetString(entry, "sAMAccountName") ?? account;
            var displayName = GetString(entry, "displayName")
                ?? GetString(entry, "name")
                ?? qualified;
            var email = GetString(entry, "mail");
            var sidBytes = GetBytes(entry, "objectSid");
            var sid = sidBytes is null ? qualified : ConvertSidToString(sidBytes);

            return new ActiveDirectoryIdentity(
                UserName: sam,
                DomainQualifiedName: string.IsNullOrWhiteSpace(domainName) ? sam : $"{domainName}\\{sam}",
                DisplayName: displayName,
                Email: email,
                ExternalId: $"ad:{sid}");
        }
        catch (LdapException exception) when (exception.ErrorCode == 49)
        {
            // 49 = invalidCredentials
            _logger.LogInformation("LDAP bind rejected credentials for {UserName}", userName);
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "LDAP authentication failed for {UserName} against {Server}:{Port}", userName, server, port);
            return null;
        }
    }

    private static string BuildBindName(string? domain, string account)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return account;
        }

        return domain.Contains('.', StringComparison.Ordinal)
            ? $"{account}@{domain}"
            : $"{domain}\\{account}";
    }

    private static string ResolveDefaultNamingContext(LdapConnection connection)
    {
        var request = new SearchRequest(
            string.Empty,
            "(objectClass=*)",
            SearchScope.Base,
            "defaultNamingContext");

        var response = (SearchResponse)connection.SendRequest(request);
        if (response.Entries.Count == 0)
        {
            throw new InvalidOperationException("RootDSE query returned no entries; set WindowsAuth:LdapBaseDn explicitly.");
        }

        return GetString(response.Entries[0], "defaultNamingContext")
            ?? throw new InvalidOperationException("RootDSE has no defaultNamingContext; set WindowsAuth:LdapBaseDn explicitly.");
    }

    private static SearchResultEntry? FindUser(LdapConnection connection, string baseDn, string account)
    {
        var filter = $"(&(objectClass=user)(sAMAccountName={EscapeFilterValue(account)}))";
        var request = new SearchRequest(baseDn, filter, SearchScope.Subtree, UserAttributes);
        var response = (SearchResponse)connection.SendRequest(request);
        return response.Entries.Count > 0 ? response.Entries[0] : null;
    }

    private static string? GetString(SearchResultEntry? entry, string attribute)
    {
        if (entry is null || !entry.Attributes.Contains(attribute))
        {
            return null;
        }

        var values = entry.Attributes[attribute].GetValues(typeof(string));
        if (values.Length == 0)
        {
            return null;
        }

        var value = values[0] as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static byte[]? GetBytes(SearchResultEntry? entry, string attribute)
    {
        if (entry is null || !entry.Attributes.Contains(attribute))
        {
            return null;
        }

        var values = entry.Attributes[attribute].GetValues(typeof(byte[]));
        return values.Length == 0 ? null : values[0] as byte[];
    }

    /// <summary>Converts a binary AD objectSid into "S-1-5-21-..." form (same as SecurityIdentifier.Value).</summary>
    private static string ConvertSidToString(byte[] sid)
    {
        if (sid.Length < 8)
        {
            return Convert.ToHexString(sid);
        }

        var revision = sid[0];
        var subAuthorityCount = sid[1];
        ulong authority = 0;
        for (var i = 2; i < 8; i++)
        {
            authority = (authority << 8) | sid[i];
        }

        var builder = new StringBuilder();
        builder.Append("S-").Append(revision).Append('-').Append(authority);

        var offset = 8;
        for (var i = 0; i < subAuthorityCount && offset + 4 <= sid.Length; i++, offset += 4)
        {
            var subAuthority = BitConverter.ToUInt32(sid, offset);
            builder.Append('-').Append(subAuthority.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string EscapeFilterValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\5c");
                    break;
                case '*':
                    builder.Append("\\2a");
                    break;
                case '(':
                    builder.Append("\\28");
                    break;
                case ')':
                    builder.Append("\\29");
                    break;
                case '\0':
                    builder.Append("\\00");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
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
