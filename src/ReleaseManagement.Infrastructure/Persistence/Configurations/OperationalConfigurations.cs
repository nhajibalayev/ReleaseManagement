using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Infrastructure.Persistence.Configurations;

public sealed class DeploymentRecordConfiguration :
    IEntityTypeConfiguration<DeploymentRecord>
{
    public void Configure(EntityTypeBuilder<DeploymentRecord> builder)
    {
        builder.ToTable("DeploymentRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.PipelineUrl).HasMaxLength(2048);
        builder.Property(x => x.PipelineRunId).HasMaxLength(200);
        builder.Property(x => x.BuildNumber).HasMaxLength(100);
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.ErrorDetails).HasMaxLength(8000);
        builder.Property(x => x.RollbackResult).HasMaxLength(4000);
        builder.Property(x => x.StartedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.ReleaseId, x.StartedDate });
        builder.HasIndex(x => x.Status);
        builder.HasOne<DeploymentEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AzureDevOpsMappingConfiguration :
    IEntityTypeConfiguration<AzureDevOpsMapping>
{
    public void Configure(EntityTypeBuilder<AzureDevOpsMapping> builder)
    {
        builder.ToTable("AzureDevOpsMappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizationUrl).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.ProjectName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WorkItemUrl).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.LastSynchronizationStatus).HasConversion<int>();
        builder.Property(x => x.LastSynchronizationError).HasMaxLength(4000);
        builder.Property(x => x.LastSynchronizedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.ReleaseId).IsUnique();
        builder.HasIndex(x => new { x.OrganizationUrl, x.ProjectName, x.WorkItemId }).IsUnique();
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.NotificationType).HasConversion<int>();
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReadDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.EmailSentDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedDate });
        builder.HasIndex(x => x.ReleaseId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValuesJson).HasColumnType("jsonb");
        builder.Property(x => x.NewValuesJson).HasColumnType("jsonb");
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(1000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedDate);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
