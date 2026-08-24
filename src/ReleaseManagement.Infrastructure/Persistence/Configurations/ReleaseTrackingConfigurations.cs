using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Infrastructure.Persistence.Configurations;

public sealed class ReleaseServiceConfiguration : IEntityTypeConfiguration<ReleaseService>
{
    public void Configure(EntityTypeBuilder<ReleaseService> builder)
    {
        builder.ToTable("ReleaseServices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchName).HasMaxLength(255);
        builder.Property(x => x.CommitId).HasMaxLength(100);
        builder.Property(x => x.Version).HasMaxLength(100);
        builder.Property(x => x.BuildNumber).HasMaxLength(100);
        builder.Property(x => x.ArtifactUrl).HasMaxLength(2048);
        builder.Property(x => x.RepositoryUrl).HasMaxLength(2048);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasIndex(x => new { x.ReleaseId, x.ServiceId }).IsUnique();
        builder.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReleaseWorkItemConfiguration : IEntityTypeConfiguration<ReleaseWorkItem>
{
    public void Configure(EntityTypeBuilder<ReleaseWorkItem> builder)
    {
        builder.ToTable("ReleaseWorkItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkItemType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.WorkItemUrl).HasMaxLength(2048).IsRequired();
        builder.HasIndex(x => new { x.ReleaseId, x.WorkItemId }).IsUnique();
    }
}

public sealed class ReleaseApprovalConfiguration : IEntityTypeConfiguration<ReleaseApproval>
{
    public void Configure(EntityTypeBuilder<ReleaseApproval> builder)
    {
        builder.ToTable("ReleaseApprovals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ApprovalType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.AssignedRole).HasMaxLength(100);
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.RequestedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DecisionDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.ReleaseId, x.ApprovalType, x.Sequence });
        builder.HasIndex(x => new { x.AssignedUserId, x.Status });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecisionByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReleaseCommentConfiguration : IEntityTypeConfiguration<ReleaseComment>
{
    public void Configure(EntityTypeBuilder<ReleaseComment> builder)
    {
        builder.ToTable("ReleaseComments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Comment).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.CommentType).HasConversion<int>();
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.ReleaseId, x.CreatedDate });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReleaseComment>().WithMany().HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReleaseAttachmentConfiguration :
    IEntityTypeConfiguration<ReleaseAttachment>
{
    public void Configure(EntityTypeBuilder<ReleaseAttachment> builder)
    {
        builder.ToTable("ReleaseAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.AttachmentType).HasConversion<int>();
        builder.Property(x => x.UploadedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.ReleaseId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReleaseStatusHistoryConfiguration :
    IEntityTypeConfiguration<ReleaseStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReleaseStatusHistory> builder)
    {
        builder.ToTable("ReleaseStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<int>();
        builder.Property(x => x.ToStatus).HasConversion<int>();
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.ResponsibleRole).HasMaxLength(100);
        builder.Property(x => x.ChangedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.ReleaseId, x.ChangedDate });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
