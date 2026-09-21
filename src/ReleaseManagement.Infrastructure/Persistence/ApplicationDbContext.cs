using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Domain.Entities;
using ReleaseManagement.Infrastructure.Identity;

namespace ReleaseManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<AppIdentityUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>,
      IApplicationDbContext
{
    private readonly ILogger<ApplicationDbContext>? _logger;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ILogger<ApplicationDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
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

    public DbSet<ReleaseReference> ReleaseReferences => Set<ReleaseReference>();

    public DbSet<ReadinessControl> ReadinessControls => Set<ReadinessControl>();

    public DbSet<ReleaseCommunication> ReleaseCommunications => Set<ReleaseCommunication>();

    public DbSet<PostReleaseValidation> PostReleaseValidations => Set<PostReleaseValidation>();

    public DbSet<PostImplementationReview> PostImplementationReviews => Set<PostImplementationReview>();

    public DbSet<PirAction> PirActions => Set<PirAction>();

    public DbSet<ReleaseForecast> ReleaseForecasts => Set<ReleaseForecast>();

    public DbSet<FreezePeriod> FreezePeriods => Set<FreezePeriod>();

    public DbSet<FreezeException> FreezeExceptions => Set<FreezeException>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<Role> AppRoles => Set<Role>();

    public DbSet<UserRole> AppUserRoles => Set<UserRole>();

    public Task ReloadAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
        => Entry(entity).ReloadAsync(cancellationToken);

    public void Detach<TEntity>(TEntity entity)
        where TEntity : class
    {
        var entry = Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    public void ForceAdded<TEntity>(TEntity entity)
        where TEntity : class
    {
        var entry = Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            Set<TEntity>().Add(entity);
            return;
        }

        entry.State = EntityState.Added;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.HasSequence<long>("release_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);
    }
}
