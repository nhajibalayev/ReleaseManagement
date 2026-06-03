using Microsoft.AspNetCore.Authentication;

namespace ReleaseHub.Api.Auth;

public sealed class SsoAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "Sso";
}
