using System.ComponentModel.DataAnnotations;

namespace ReleaseHub.Web.Models;

public sealed class LoginViewModel
{
    public string SsoLoginUrl { get; init; } = string.Empty;
}

public sealed class LoginFormModel
{
    [Required]
    [Display(Name = "SSO access token")]
    public string AccessToken { get; set; } = string.Empty;
}
