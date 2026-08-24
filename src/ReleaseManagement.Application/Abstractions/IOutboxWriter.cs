namespace ReleaseManagement.Application.Abstractions;

public static class OutboxMessageTypes
{
    public const string AzureDevOpsCreateWorkItem = "AzureDevOps.CreateWorkItem";
    public const string AzureDevOpsUpdateWorkItem = "AzureDevOps.UpdateWorkItem";
    public const string SendEmailNotification = "Notification.SendEmail";
}

public interface IOutboxWriter
{
    Task EnqueueAsync(
        string messageType,
        string payloadJson,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
