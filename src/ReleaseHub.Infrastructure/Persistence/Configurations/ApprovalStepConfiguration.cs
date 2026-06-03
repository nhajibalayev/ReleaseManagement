using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Infrastructure.Persistence.Configurations;

public sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> b)
    {
        b.ToTable("approval_steps");
        b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.Decision).HasConversion<int>();
        b.Property(x => x.ApproverUserId).HasMaxLength(200);
        b.Property(x => x.ApproverEmail).HasMaxLength(320);
        b.Property(x => x.Comment).HasMaxLength(4000);
        b.HasIndex(x => new { x.ReleaseTaskId, x.StepOrder }).IsUnique();
    }
}
