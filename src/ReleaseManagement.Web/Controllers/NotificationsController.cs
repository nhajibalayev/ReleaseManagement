using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;

    public NotificationsController(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Notifications.AsNoTracking()
            .Where(item => item.UserId == _currentUser.UserId)
            .OrderByDescending(item => item.CreatedDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, _currentUser.UserId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Latest(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Notifications.AsNoTracking()
            .Where(item => item.UserId == _currentUser.UserId)
            .OrderByDescending(item => item.CreatedDate)
            .Take(8)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Message,
                item.IsRead,
                item.CreatedDate,
                item.ReleaseId
            })
            .ToListAsync(cancellationToken);

        var unread = await _dbContext.Notifications.AsNoTracking()
            .CountAsync(
                item => item.UserId == _currentUser.UserId && !item.IsRead,
                cancellationToken);

        return Json(new { unread, items });
    }
}
