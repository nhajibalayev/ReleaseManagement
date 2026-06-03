using Microsoft.EntityFrameworkCore;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<ReleaseTask> ReleaseTasks { get; }
    DbSet<ProjectScope> ProjectScopes { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
