using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Identity;

namespace ReleaseManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<AppIdentityUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>,
      IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Release> Releases => Set<Release>();

    public DbSet<ReleaseService> ReleaseServices => Set<ReleaseService>();

    public DbSet<ReleaseApproval> ReleaseApprovals => Set<ReleaseApproval>();

    public DbSet<ReleaseStatusHistory> ReleaseStatusHistories => Set<ReleaseStatusHistory>();

    public DbSet<ReleaseComment> ReleaseComments => Set<ReleaseComment>();

    public DbSet<ReleaseAttachment> ReleaseAttachments => Set<ReleaseAttachment>();

    public DbSet<ReleaseWorkItem> ReleaseWorkItems => Set<ReleaseWorkItem>();

    public DbSet<DeploymentRecord> DeploymentRecords => Set<DeploymentRecord>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<DeploymentEnvironment> Environments => Set<DeploymentEnvironment>();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    DbSet<ApplicationUser> IApplicationDbContext.Users => ApplicationUsers;

    public DbSet<UserProductAccess> UserProductAccesses => Set<UserProductAccess>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<AzureDevOpsMapping> AzureDevOpsMappings => Set<AzureDevOpsMapping>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<Role> AppRoles => Set<Role>();

    public DbSet<UserRole> AppUserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.HasSequence<long>("release_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);
    }
}
