using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public sealed record AzureDevOpsUserCredential(string UserName, string Password)
{
    public NetworkCredential ToNetworkCredential()
    {
        var (domain, account) = SplitUserName(UserName);
        return string.IsNullOrWhiteSpace(domain)
            ? new NetworkCredential(account, Password)
            : new NetworkCredential(account, Password, domain);
    }

    private static (string? Domain, string Account) SplitUserName(string userName)
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

        return (null, userName);
    }
}

/// <summary>
/// Authenticates to on-prem Azure DevOps Server with the signed-in user's AD
/// credentials via NTLM/Negotiate (Basic AD password auth is usually disabled on IIS).
/// </summary>
public sealed class AzureDevOpsAuthHandler : HttpMessageHandler
{
    private readonly IAzureDevOpsUserCredentialStore _credentialStore;
    private readonly AzureDevOpsOptions _options;

    public AzureDevOpsAuthHandler(
        IAzureDevOpsUserCredentialStore credentialStore,
        IOptions<AzureDevOpsOptions> options)
    {
        _credentialStore = credentialStore;
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // New handler per request so concurrent users do not share Credentials.
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        };

        var credential = _credentialStore.Get();
        if (credential is not null)
        {
            handler.Credentials = credential.ToNetworkCredential();
            handler.PreAuthenticate = true;
        }
        else if (_options.UseWindowsCredentials)
        {
            handler.UseDefaultCredentials = true;
            handler.PreAuthenticate = true;
        }
        else if (!string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            var basic = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }

        // Buffer the response so the per-request handler can be disposed safely.
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var response = await invoker.SendAsync(request, cancellationToken);
        var buffered = new HttpResponseMessage(response.StatusCode)
        {
            ReasonPhrase = response.ReasonPhrase,
            Version = response.Version,
            RequestMessage = request,
            Content = new ByteArrayContent(
                response.Content is null
                    ? []
                    : await response.Content.ReadAsByteArrayAsync(cancellationToken))
        };

        foreach (var header in response.Headers)
        {
            buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                buffered.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return buffered;
    }
}

internal static class AzureDevOpsAuthorization
{
    public static bool TryApply(
        HttpRequestMessage request,
        string? authorizationValue,
        AzureDevOpsOptions options,
        IAzureDevOpsUserCredentialStore? credentialStore = null)
    {
        // AD session credentials are applied by AzureDevOpsAuthHandler (NTLM).
        if (credentialStore?.Get() is not null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(authorizationValue))
        {
            // Ignore legacy "Basic DOMAIN\user:password" — on-prem rejects it with 401.
            if (authorizationValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) &&
                !IsPatBasic(authorizationValue, options))
            {
                return credentialStore?.Get() is not null || options.UseWindowsCredentials;
            }

            var separator = authorizationValue.IndexOf(' ');
            if (separator > 0)
            {
                var scheme = authorizationValue[..separator];
                var parameter = authorizationValue[(separator + 1)..];
                request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
                return true;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationValue);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            var basic = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($":{options.PersonalAccessToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            return true;
        }

        return options.UseWindowsCredentials;
    }

    private static bool IsPatBasic(string authorizationValue, AzureDevOpsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            return false;
        }

        var expected = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($":{options.PersonalAccessToken}"));
        return authorizationValue.Equals($"Basic {expected}", StringComparison.Ordinal);
    }
}
