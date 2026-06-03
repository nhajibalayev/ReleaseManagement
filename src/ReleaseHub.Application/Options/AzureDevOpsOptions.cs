namespace ReleaseHub.Application.Options;

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";
    public string BaseUrl { get; set; } = string.Empty;
    public string Collection { get; set; } = "DefaultCollection";
    public string Project { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = "Release Task";
    public string PersonalAccessToken { get; set; } = string.Empty;
}
