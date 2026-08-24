using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Infrastructure.Persistence.Configurations;

public sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("Releases");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReleaseNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ReleaseVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessReason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ImpactDescription).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RiskDescription).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.DeploymentPlan).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.RollbackPlan).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.TestingSummary).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.MonitoringPlan).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.PostReleaseValidationPlan).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CurrentResponsibleRole).HasMaxLength(100);
        builder.Property(x => x.AzureDevOpsWorkItemUrl).HasMaxLength(2048);

        builder.Property(x => x.ReleaseType).HasConversion<int>();
        builder.Property(x => x.CurrentStatus).HasConversion<int>();
        builder.Property(x => x.Priority).HasConversion<int>();
        builder.Property(x => x.RiskLevel).HasConversion<int>();

        ConfigureUtc(builder.Property(x => x.PlannedReleaseDate));
        ConfigureUtc(builder.Property(x => x.ActualReleaseDate));
        ConfigureUtc(builder.Property(x => x.SubmittedDate));
        ConfigureUtc(builder.Property(x => x.ClosedDate));
        ConfigureUtc(builder.Property(x => x.CreatedDate));
        ConfigureUtc(builder.Property(x => x.UpdatedDate));

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.ReleaseNumber).IsUnique();
        builder.HasIndex(x => x.CurrentStatus);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.CurrentResponsibleUserId);
        builder.HasIndex(x => x.PlannedReleaseDate);
        builder.HasIndex(x => x.ProductId);

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeploymentEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CurrentResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureCollection(builder, x => x.Services, "_services", DeleteBehavior.Cascade);
        ConfigureCollection(builder, x => x.WorkItems, "_workItems", DeleteBehavior.Cascade);
        ConfigureCollection(builder, x => x.Approvals, "_approvals", DeleteBehavior.Restrict);
        ConfigureCollection(builder, x => x.Comments, "_comments", DeleteBehavior.Restrict);
        ConfigureCollection(builder, x => x.Attachments, "_attachments", DeleteBehavior.Restrict);
        ConfigureCollection(builder, x => x.StatusHistory, "_statusHistory", DeleteBehavior.Restrict);
        ConfigureCollection(builder, x => x.DeploymentRecords, "_deploymentRecords", DeleteBehavior.Restrict);
    }

    private static void ConfigureCollection<T>(
        EntityTypeBuilder<Release> builder,
        System.Linq.Expressions.Expression<Func<Release, IEnumerable<T>?>> navigationExpression,
        string fieldName,
        DeleteBehavior deleteBehavior)
        where T : class
    {
        builder.HasMany(navigationExpression)
            .WithOne()
            .HasForeignKey("ReleaseId")
            .OnDelete(deleteBehavior);

        builder.Navigation(navigationExpression)
            .HasField(fieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureUtc(PropertyBuilder<DateTime> property) =>
        property.HasColumnType("timestamp with time zone");

    private static void ConfigureUtc(PropertyBuilder<DateTime?> property) =>
        property.HasColumnType("timestamp with time zone");
}
