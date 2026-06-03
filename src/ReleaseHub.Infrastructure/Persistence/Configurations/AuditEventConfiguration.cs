using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("audit_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Actor).HasMaxLength(320).IsRequired();
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.Payload).HasColumnType("jsonb");
        b.HasIndex(x => x.ReleaseTaskId);
    }
}
