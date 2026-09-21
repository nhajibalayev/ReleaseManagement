using Microsoft.EntityFrameworkCore;
using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Release> Releases { get; }

    DbSet<ReleaseService> ReleaseServices { get; }

    DbSet<ReleaseApproval> ReleaseApprovals { get; }

    DbSet<ReleaseStatusHistory> ReleaseStatusHistories { get; }

    DbSet<ReleaseComment> ReleaseComments { get; }

    DbSet<ReleaseAttachment> ReleaseAttachments { get; }

    DbSet<ReleaseWorkItem> ReleaseWorkItems { get; }

    DbSet<DeploymentRecord> DeploymentRecords { get; }

    DbSet<Product> Products { get; }

    DbSet<Service> Services { get; }

    DbSet<DeploymentEnvironment> Environments { get; }

    DbSet<ApplicationUser> Users { get; }

    DbSet<UserProductAccess> UserProductAccesses { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<AzureDevOpsMapping> AzureDevOpsMappings { get; }

    // Procedure v4.0 alignment.
    DbSet<ReleaseReference> ReleaseReferences { get; }

    DbSet<ReadinessControl> ReadinessControls { get; }

    DbSet<ReleaseCommunication> ReleaseCommunications { get; }

    DbSet<PostReleaseValidation> PostReleaseValidations { get; }

    DbSet<PostImplementationReview> PostImplementationReviews { get; }

    DbSet<PirAction> PirActions { get; }

    DbSet<ReleaseForecast> ReleaseForecasts { get; }

    DbSet<FreezePeriod> FreezePeriods { get; }

    DbSet<FreezeException> FreezeExceptions { get; }

    Task ReloadAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class;

    void Detach<TEntity>(TEntity entity)
        where TEntity : class;

    void ForceAdded<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
