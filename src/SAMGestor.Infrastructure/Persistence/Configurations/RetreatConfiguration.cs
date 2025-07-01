using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class RetreatConfiguration : IEntityTypeConfiguration<Retreat>
{
    public void Configure(EntityTypeBuilder<Retreat> builder)
    {
        builder.ToTable("retreats");
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(p => p.Value).HasColumnName("name").HasMaxLength(120).IsRequired();
        });

        builder.Property(r => r.Edition).HasColumnName("edition").HasMaxLength(30).IsRequired();
        builder.Property(r => r.Theme).HasColumnName("theme").HasMaxLength(120).IsRequired();
        builder.Property(r => r.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(r => r.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(r => r.TotalSlots).HasColumnName("total_slots").IsRequired();
        builder.Property(r => r.RegistrationStart).HasColumnName("registration_start").IsRequired();
        builder.Property(r => r.RegistrationEnd).HasColumnName("registration_end").IsRequired();

        builder.OwnsOne(r => r.WestRegionPercentage, p =>
        {
            p.Property(v => v.Value).HasColumnName("west_region_pct").HasColumnType("numeric(5,2)").IsRequired();
        });

        builder.OwnsOne(r => r.OtherRegionsPercentage, p =>
        {
            p.Property(v => v.Value).HasColumnName("other_regions_pct").HasColumnType("numeric(5,2)").IsRequired();
        });
    }
}