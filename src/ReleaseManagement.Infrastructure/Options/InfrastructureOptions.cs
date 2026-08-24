namespace ReleaseManagement.Infrastructure.Options;

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    public string OrganizationUrl { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    public string PersonalAccessToken { get; set; } = string.Empty;

    public string WorkItemType { get; set; } = "Release";

    public bool Enabled { get; set; }
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 25;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "noreply@releasemanagement.local";

    public bool Enabled { get; set; }
}

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "App_Data/attachments";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public string DefaultPassword { get; set; } = "ChangeMe!123";
}
