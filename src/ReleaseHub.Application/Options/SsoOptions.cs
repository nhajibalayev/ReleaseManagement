namespace ReleaseHub.Application.Options;

public sealed class SsoOptions
{
    public const string SectionName = "Sso";

    public string LoginUrl { get; set; } = string.Empty;
    public string UserInfoUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = "ReleaseHub";
    public int CacheTtlSeconds { get; set; } = 60;
}
