using Microsoft.EntityFrameworkCore;
using ReleaseHub.Application.Abstractions.Persistence;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ReleaseTask> ReleaseTasks => Set<ReleaseTask>();
    public DbSet<ProjectScope> ProjectScopes => Set<ProjectScope>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
