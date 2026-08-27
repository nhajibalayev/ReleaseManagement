using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Notifications;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Jobs;

public sealed class OutboxProcessorJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var azureDevOps = scope.ServiceProvider.GetRequiredService<IAzureDevOpsService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        var messages = await dbContext.OutboxMessages
            .Where(message => message.Status == "Pending" || message.Status == "Failed")
            .OrderBy(message => message.CreatedDate)
            .Take(50)
            .ToListAsync();

        foreach (var message in messages)
        {
            try
            {
                message.AttemptCount += 1;

                switch (message.MessageType)
                {
                    case OutboxMessageTypes.AzureDevOpsCreateWorkItem:
                        await HandleCreateWorkItemAsync(dbContext, azureDevOps, options, message, clock);
                        break;
                    case OutboxMessageTypes.AzureDevOpsUpdateWorkItem:
                        await HandleUpdateWorkItemAsync(dbContext, azureDevOps, options, message);
                        break;
                    case OutboxMessageTypes.SendEmailNotification:
                        await HandleEmailAsync(dbContext, emailSender, message, clock);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown outbox message type '{message.MessageType}'.");
                }

                message.Status = "Processed";
                message.ProcessedDate = clock.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.Status = "Failed";
                message.LastError = exception.Message;
                _logger.LogError(
                    exception,
                    "Outbox processing failed for {MessageId} ({MessageType})",
                    message.Id,
                    message.MessageType);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task HandleCreateWorkItemAsync(
        ApplicationDbContext dbContext,
        IAzureDevOpsService azureDevOps,
        AzureDevOpsOptions options,
        OutboxMessage message,
        IClock clock)
    {
        if (!options.Enabled)
        {
            return;
        }

        using var document = JsonDocument.Parse(message.PayloadJson);
        var releaseId = document.RootElement.GetProperty("ReleaseId").GetGuid();
        var accessToken = document.RootElement.TryGetProperty("AccessToken", out var tokenProperty)
            ? tokenProperty.GetString()
            : null;
        var release = await dbContext.Releases.SingleAsync(item => item.Id == releaseId);

        if (release.AzureDevOpsWorkItemId.HasValue)
        {
            return;
        }

        var result = await azureDevOps.CreateReleaseWorkItemAsync(release, accessToken);
        release.SetAzureDevOpsWorkItem(result.WorkItemId, result.WorkItemUrl, clock.UtcNow);

        var mapping = await dbContext.AzureDevOpsMappings
            .SingleOrDefaultAsync(item => item.ReleaseId == release.Id);

        if (mapping is null)
        {
            mapping = new AzureDevOpsMapping(
                Guid.NewGuid(),
                release.Id,
                options.OrganizationUrl,
                options.Project,
                result.WorkItemId,
                result.WorkItemUrl);
            dbContext.AzureDevOpsMappings.Add(mapping);
        }

        mapping.RecordSynchronizationSuccess(clock.UtcNow);
    }

    private static async Task HandleUpdateWorkItemAsync(
        ApplicationDbContext dbContext,
        IAzureDevOpsService azureDevOps,
        AzureDevOpsOptions options,
        OutboxMessage message)
    {
        if (!options.Enabled)
        {
            return;
        }

        using var document = JsonDocument.Parse(message.PayloadJson);
        var releaseId = document.RootElement.GetProperty("ReleaseId").GetGuid();
        var accessToken = document.RootElement.TryGetProperty("AccessToken", out var tokenProperty)
            ? tokenProperty.GetString()
            : null;
        var release = await dbContext.Releases.SingleAsync(item => item.Id == releaseId);

        if (!release.AzureDevOpsWorkItemId.HasValue)
        {
            return;
        }

        await azureDevOps.UpdateReleaseWorkItemAsync(release, accessToken);
    }

    private static async Task HandleEmailAsync(
        ApplicationDbContext dbContext,
        IEmailSender emailSender,
        OutboxMessage message,
        IClock clock)
    {
        using var document = JsonDocument.Parse(message.PayloadJson);
        var userId = document.RootElement.GetProperty("UserId").GetGuid();
        var title = document.RootElement.GetProperty("Title").GetString() ?? "Notification";
        var body = document.RootElement.GetProperty("Message").GetString() ?? string.Empty;
        var notificationId = document.RootElement.GetProperty("NotificationId").GetGuid();

        var user = await dbContext.ApplicationUsers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        await emailSender.SendAsync(user.Email, title, body);

        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notificationId);

        notification?.MarkEmailSent(clock.UtcNow);
    }
}

public sealed class TemporaryFileCleanupJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TemporaryFileCleanupJob> _logger;

    public TemporaryFileCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<TemporaryFileCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var environment = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var root = Path.IsPathRooted(options.RootPath)
            ? options.RootPath
            : Path.Combine(environment.ContentRootPath, options.RootPath);

        if (!Directory.Exists(root))
        {
            return Task.CompletedTask;
        }

        var threshold = DateTime.UtcNow.AddDays(-30);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTimeUtc < threshold)
            {
                info.Delete();
                _logger.LogInformation("Deleted stale attachment {File}", file);
            }
        }

        return Task.CompletedTask;
    }
}
