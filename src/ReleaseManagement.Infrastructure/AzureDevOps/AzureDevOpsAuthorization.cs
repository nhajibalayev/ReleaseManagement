using System.Net.Http.Headers;
using System.Text;
using ReleaseManagement.Infrastructure.Options;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

internal static class AzureDevOpsAuthorization
{
    public static bool TryApply(
        HttpRequestMessage request,
        string? authorizationValue,
        AzureDevOpsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(authorizationValue))
        {
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
}
