using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Infrastructure.Identity;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Notifications;

public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly EmailOptions _options;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email disabled. To={ToEmail}; Subject={Subject}",
                toEmail,
                subject);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Email queued. Host={Host}; To={ToEmail}; Subject={Subject}",
            _options.Host,
            toEmail,
            subject);
        return Task.CompletedTask;
    }
}

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IOutboxWriter _outbox;

    public NotificationService(
        ApplicationDbContext dbContext,
        IClock clock,
        UserManager<AppIdentityUser> userManager,
        IOutboxWriter outbox)
    {
        _dbContext = dbContext;
        _clock = clock;
        _userManager = userManager;
        _outbox = outbox;
    }

    public async Task CreateAsync(
        Guid userId,
        Guid? releaseId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            releaseId,
            title,
            message,
            type,
            _clock.UtcNow);

        _dbContext.Notifications.Add(notification);

        await _outbox.EnqueueAsync(
            OutboxMessageTypes.SendEmailNotification,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                NotificationId = notification.Id,
                UserId = userId,
                Title = title,
                Message = message
            }),
            idempotencyKey: $"email:{notification.Id}",
            cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .SingleOrDefaultAsync(
                item => item.Id == notificationId && item.UserId == userId,
                cancellationToken);

        if (notification is null)
        {
            return;
        }

        notification.MarkAsRead(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateForRoleAsync(
        string roleName,
        Guid? releaseId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        foreach (var user in users.Where(item => item.IsActive))
        {
            await CreateAsync(user.Id, releaseId, title, message, type, cancellationToken);
        }
    }
}
