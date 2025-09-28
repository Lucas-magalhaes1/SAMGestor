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
            n.Property(p => p.Value)
             .HasColumnName("name")
             .HasMaxLength(120)
             .IsRequired();
            
            n.HasIndex(p => p.Value).IsUnique();
        });

        builder.Property(r => r.Edition)
               .HasColumnName("edition")
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(r => r.Theme)
               .HasColumnName("theme")
               .HasMaxLength(120)
               .IsRequired();

        builder.Property(r => r.StartDate)
               .HasColumnName("start_date")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.EndDate)
               .HasColumnName("end_date")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.MaleSlots)
               .HasColumnName("male_slots")
               .IsRequired();

        builder.Property(r => r.FemaleSlots)
               .HasColumnName("female_slots")
               .IsRequired();

        builder.Property(r => r.RegistrationStart)
               .HasColumnName("registration_start")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(r => r.RegistrationEnd)
               .HasColumnName("registration_end")
               .HasColumnType("date")
               .IsRequired();

        builder.OwnsOne(r => r.FeeFazer, f =>
        {
            f.Property(v => v.Amount)
             .HasColumnName("fee_fazer_amount")
             .HasColumnType("numeric(18,2)")
             .IsRequired();

            f.Property(v => v.Currency)
             .HasColumnName("fee_fazer_currency")
             .HasMaxLength(3)
             .IsRequired();
        });

        builder.OwnsOne(r => r.FeeServir, f =>
        {
            f.Property(v => v.Amount)
             .HasColumnName("fee_servir_amount")
             .HasColumnType("numeric(18,2)")
             .IsRequired();

            f.Property(v => v.Currency)
             .HasColumnName("fee_servir_currency")
             .HasMaxLength(3)
             .IsRequired();
        });

        builder.OwnsOne(r => r.WestRegionPercentage, p =>
        {
            p.Property(v => v.Value)
             .HasColumnName("west_region_pct")
             .HasColumnType("numeric(5,2)")
             .IsRequired();
        });

        builder.OwnsOne(r => r.OtherRegionsPercentage, p =>
        {
            p.Property(v => v.Value)
             .HasColumnName("other_regions_pct")
             .HasColumnType("numeric(5,2)")
             .IsRequired();
        });

        builder.Property(r => r.ContemplationClosed)
               .HasColumnName("contemplation_closed")
               .IsRequired();

        builder.Property(r => r.FamiliesVersion)
               .HasColumnName("families_version")
               .IsRequired()
               .IsConcurrencyToken();
        
        builder.Property(r => r.FamiliesLocked)
               .HasColumnName("families_locked")
               .HasDefaultValue(false)
               .IsRequired();
        
        builder.Property(x => x.ServiceSpacesVersion)
               .HasColumnName("service_spaces_version")
               .HasDefaultValue(0)
               .IsRequired()
               .IsConcurrencyToken();

        builder.Property(x => x.ServiceLocked)
               .HasColumnName("service_locked")
               .HasDefaultValue(false)
               .IsRequired();
    }
}
