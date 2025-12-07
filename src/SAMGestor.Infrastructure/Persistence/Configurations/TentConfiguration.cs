using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;


namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class TentConfiguration : IEntityTypeConfiguration<Tent>
{
    public void Configure(EntityTypeBuilder<Tent> b)
    {
        b.ToTable("tents", t =>
        {
            t.HasCheckConstraint("ck_tents_capacity_positive", "capacity > 0");
        });

        b.HasKey(t => t.Id);
    
        b.OwnsOne(t => t.Number, n =>
        {
            n.Property(p => p.Value)
                .HasColumnName("number")
                .IsRequired();
        });

        b.Property(t => t.Category)
            .HasColumnName("category")
            .HasConversion<string>()   
            .HasMaxLength(16)
            .IsRequired();

        b.Property(t => t.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        b.Property(t => t.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        b.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        b.Property(t => t.IsLocked)
            .HasColumnName("is_locked")
            .HasDefaultValue(false)
            .IsRequired();

        b.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(280);

        b.HasIndex(t => t.RetreatId);
    }
}