using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Infrastructure.Persistence.Configurations;

public sealed class ReleaseTaskConfiguration : IEntityTypeConfiguration<ReleaseTask>
{
    public void Configure(EntityTypeBuilder<ReleaseTask> b)
    {
        b.ToTable("release_tasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.AdoStateMirror).HasMaxLength(100);
        b.Property(x => x.CreatedByUserId).HasMaxLength(200).IsRequired();
        b.Property(x => x.CreatedByEmail).HasMaxLength(320).IsRequired();
        b.Property(x => x.State).HasConversion<int>();
        b.HasIndex(x => x.AdoWorkItemId).IsUnique().HasFilter("\"AdoWorkItemId\" IS NOT NULL");
        b.HasMany(x => x.ProjectScopes).WithOne().HasForeignKey(x => x.ReleaseTaskId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.ApprovalSteps).WithOne().HasForeignKey(x => x.ReleaseTaskId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.AuditEvents).WithOne().HasForeignKey(x => x.ReleaseTaskId).OnDelete(DeleteBehavior.Cascade);
    }
}
