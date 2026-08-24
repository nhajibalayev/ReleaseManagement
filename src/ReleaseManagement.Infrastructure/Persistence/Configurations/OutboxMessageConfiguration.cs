using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseManagement.Infrastructure.Persistence;

namespace ReleaseManagement.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration :
    IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageType).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(8000);
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ProcessedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.CreatedDate });
    }
}
