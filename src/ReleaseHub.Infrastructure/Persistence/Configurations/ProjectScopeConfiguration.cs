using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseHub.Domain.Entities;

namespace ReleaseHub.Infrastructure.Persistence.Configurations;

public sealed class ProjectScopeConfiguration : IEntityTypeConfiguration<ProjectScope>
{
    public void Configure(EntityTypeBuilder<ProjectScope> b)
    {
        b.ToTable("project_scopes");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProjectKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.Repository).HasMaxLength(200);
        b.Property(x => x.Reference).HasMaxLength(200);
        b.Property(x => x.ChangeSummary).HasMaxLength(2000);
    }
}
