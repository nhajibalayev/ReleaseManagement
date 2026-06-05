using Microsoft.EntityFrameworkCore;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Application.Abstractions.Persistence;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Application.ReleaseTasks;

public sealed class ReleaseTaskService : IReleaseTaskService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ReleaseTaskService(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ReleaseTaskDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.ReleaseTasks
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => Map(t))
            .ToListAsync(ct);
    }

    public async Task<ReleaseTaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ReleaseTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<ReleaseTaskDto> CreateAsync(CreateReleaseTaskRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        var user = _currentUser.User;
        var entity = new ReleaseTask
        {
            Title = request.Title.Trim(),
            Description = request.Description ?? string.Empty,
            CreatedByUserId = user?.UserId ?? string.Empty,
            CreatedByEmail = user?.Email ?? string.Empty
        };

        _db.ReleaseTasks.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static ReleaseTaskDto Map(ReleaseTask t) => new()
    {
        Id = t.Id,
        AdoWorkItemId = t.AdoWorkItemId,
        Title = t.Title,
        Description = t.Description,
        State = t.State,
        CreatedByEmail = t.CreatedByEmail,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
