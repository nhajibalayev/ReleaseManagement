using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseManagement.Domain.Entities;

namespace ReleaseManagement.Infrastructure.Persistence.Configurations;

public sealed class ReleaseReferenceConfiguration : IEntityTypeConfiguration<ReleaseReference>
{
    public void Configure(EntityTypeBuilder<ReleaseReference> builder)
    {
        builder.ToTable("ReleaseReferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferenceType).HasConversion<int>();
        builder.Property(x => x.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.AddedDate).HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.IsSourceReference);
        builder.HasIndex(x => new { x.ReleaseId, x.ReferenceType });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReadinessControlConfiguration : IEntityTypeConfiguration<ReadinessControl>
{
    public void Configure(EntityTypeBuilder<ReadinessControl> builder)
    {
        builder.ToTable("ReadinessControls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ControlType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.OwnerRole).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EvidenceReference).HasMaxLength(2048);
        builder.Property(x => x.Justification).HasMaxLength(4000);
        builder.Property(x => x.UpdatedDate).HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.IsClosed);
        builder.HasIndex(x => new { x.ReleaseId, x.ControlType }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.IsRequired });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReleaseCommunicationConfiguration : IEntityTypeConfiguration<ReleaseCommunication>
{
    public void Configure(EntityTypeBuilder<ReleaseCommunication> builder)
    {
        builder.ToTable("ReleaseCommunications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CommunicationType).HasConversion<int>();
        builder.Property(x => x.Audience).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SentDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.ReleaseId, x.SentDate });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PostReleaseValidationConfiguration : IEntityTypeConfiguration<PostReleaseValidation>
{
    public void Configure(EntityTypeBuilder<PostReleaseValidation> builder)
    {
        builder.ToTable("PostReleaseValidations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TechnicalResult).HasConversion<int>();
        builder.Property(x => x.BusinessResult).HasConversion<int>();
        builder.Property(x => x.TechnicalNotes).HasMaxLength(4000);
        builder.Property(x => x.TechnicalEvidenceReference).HasMaxLength(2048);
        builder.Property(x => x.BusinessNotes).HasMaxLength(4000);
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.TechnicalValidatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.BusinessValidatedDate).HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.IsComplete);
        builder.Ignore(x => x.IsPassed);
        builder.HasIndex(x => x.ReleaseId).IsUnique();
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicalValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.BusinessValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PostImplementationReviewConfiguration : IEntityTypeConfiguration<PostImplementationReview>
{
    public void Configure(EntityTypeBuilder<PostImplementationReview> builder)
    {
        builder.ToTable("PostImplementationReviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Triggers).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Format).HasConversion<int>();
        builder.Property(x => x.RootCause).HasMaxLength(8000);
        builder.Property(x => x.LessonsLearned).HasMaxLength(8000);
        builder.Property(x => x.BacklogReference).HasMaxLength(2048);
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.ReleaseId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Actions)
            .WithOne()
            .HasForeignKey(x => x.PostImplementationReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Actions)
            .HasField("_actions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PirActionConfiguration : IEntityTypeConfiguration<PirAction>
{
    public void Configure(EntityTypeBuilder<PirAction> builder)
    {
        builder.ToTable("PirActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.OwnerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(2048);
        builder.Property(x => x.TargetDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.PostImplementationReviewId);
    }
}

public sealed class ReleaseForecastConfiguration : IEntityTypeConfiguration<ReleaseForecast>
{
    public void Configure(EntityTypeBuilder<ReleaseForecast> builder)
    {
        builder.ToTable("ReleaseForecasts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Team).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Dependencies).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ExpectedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.Year, x.Quarter });
        builder.HasIndex(x => x.ProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FreezePeriodConfiguration : IEntityTypeConfiguration<FreezePeriod>
{
    public void Configure(EntityTypeBuilder<FreezePeriod> builder)
    {
        builder.ToTable("FreezePeriods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FreezeType).HasConversion<int>();
        builder.Property(x => x.Authority).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.StartDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.EndDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.IsActive, x.StartDate, x.EndDate });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Exceptions)
            .WithOne()
            .HasForeignKey(x => x.FreezePeriodId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Exceptions)
            .HasField("_exceptions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FreezeExceptionConfiguration : IEntityTypeConfiguration<FreezeException>
{
    public void Configure(EntityTypeBuilder<FreezeException> builder)
    {
        builder.ToTable("FreezeExceptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ApprovedBy).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ApprovedDate).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.FreezePeriodId, x.ReleaseId }).IsUnique();
        builder.HasOne<Release>().WithMany().HasForeignKey(x => x.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
