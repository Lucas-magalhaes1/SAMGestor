using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new FamilyName(fromProvider))
            .IsRequired();

        builder.Property(f => f.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.Property(f => f.Capacity)
            .HasColumnName("capacity")
            .IsRequired();
        
        builder.Property(f => f.IsLocked)                  
            .HasColumnName("is_locked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(f => f.RetreatId);

        builder.Navigation(f => f.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        
        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(f => f.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_families_capacity_positive", "capacity > 0");
        });
        
        builder.HasIndex(f => new { f.RetreatId, f.Name }).IsUnique();
    }
}