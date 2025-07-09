using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class RegionConfigConfiguration : IEntityTypeConfiguration<RegionConfig>
{
    public void Configure(EntityTypeBuilder<RegionConfig> builder)
    {
        builder.ToTable("region_configs");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        builder.Property(r => r.Observation).HasColumnName("observation").HasMaxLength(200);

        builder.Property(r => r.RetreatId).HasColumnName("retreat_id").IsRequired();

        builder.OwnsOne(r => r.TargetPercentage, p =>
        {
            p.Property(v => v.Value).HasColumnName("target_percentage").HasColumnType("numeric(5,2)").IsRequired();
        });

        builder.HasIndex(r => new { r.RetreatId, r.Name }).IsUnique();
    }
}