using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Application.Options;

namespace ReleaseHub.Infrastructure.Sso;

public sealed class SsoClient : ISsoClient
{
    public const string HttpClientName = "Sso";
    private readonly HttpClient _http;
    private readonly SsoOptions _options;
    private readonly ILogger<SsoClient> _log;

    public SsoClient(HttpClient http, IOptions<SsoOptions> options, ILogger<SsoClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<CurrentUser?> ValidateAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.UserInfoUrl))
        {
            _log.LogWarning("Sso:UserInfoUrl is not configured.");
            return null;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoUrl);
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("SSO validation failed: {Status}", resp.StatusCode);
            return null;
        }

        var payload = await resp.Content.ReadFromJsonAsync<SsoResponse>(cancellationToken: ct);
        if (payload is null || string.IsNullOrEmpty(payload.UserId)) return null;

        return new CurrentUser
        {
            UserId = payload.UserId,
            Email = payload.Email ?? string.Empty,
            DisplayName = payload.DisplayName ?? string.Empty,
            Roles = payload.Roles ?? Array.Empty<string>()
        };
    }

    private sealed class SsoResponse
    {
        [JsonPropertyName("userId")] public string UserId { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("roles")] public string[]? Roles { get; set; }
    }
}
