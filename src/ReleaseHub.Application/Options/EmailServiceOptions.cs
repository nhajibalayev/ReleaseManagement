namespace ReleaseHub.Application.Options;

public sealed class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    public string BaseUrl { get; set; } = string.Empty;
    public string SendPath { get; set; } = "/api/email/send";
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "releasehub@local";
}
