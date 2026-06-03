using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.Auth;

namespace ReleaseHub.Api.Auth;

public sealed class SsoAuthenticationHandler : AuthenticationHandler<SsoAuthenticationOptions>
{
    private readonly ISsoClient _ssoClient;

    public SsoAuthenticationHandler(
        IOptionsMonitor<SsoAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISsoClient ssoClient)
        : base(options, logger, encoder)
    {
        _ssoClient = ssoClient;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
            return AuthenticateResult.NoResult();

        var value = header.ToString();
        if (string.IsNullOrEmpty(value) || !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = value["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.Fail("Empty bearer token.");

        var user = await _ssoClient.ValidateAsync(token, Context.RequestAborted);
        if (user is null)
            return AuthenticateResult.Fail("SSO token validation failed.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
