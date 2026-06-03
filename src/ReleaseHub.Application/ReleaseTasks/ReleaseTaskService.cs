using Microsoft.EntityFrameworkCore;
using ReleaseHub.Application.Abstractions.Persistence;

namespace ReleaseHub.Application.ReleaseTasks;

public sealed class ReleaseTaskService : IReleaseTaskService
{
    private readonly IAppDbContext _db;

    public ReleaseTaskService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReleaseTaskDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.ReleaseTasks
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ReleaseTaskDto
            {
                Id = t.Id,
                AdoWorkItemId = t.AdoWorkItemId,
                Title = t.Title,
                State = t.State,
                CreatedByEmail = t.CreatedByEmail,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);
    }
}
