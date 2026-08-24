using ReleaseManagement.Domain.Enums;

namespace ReleaseManagement.Application.Abstractions;

public interface INotificationService
{
    Task CreateAsync(
        Guid userId,
        Guid? releaseId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task CreateForRoleAsync(
        string roleName,
        Guid? releaseId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default);
}
