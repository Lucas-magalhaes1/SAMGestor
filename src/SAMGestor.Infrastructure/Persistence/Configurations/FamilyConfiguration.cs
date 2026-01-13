using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
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

        builder.OwnsOne(f => f.Color, colorBuilder =>
        {
            colorBuilder.Property(c => c.Name)
                .HasColumnName("color_name")
                .HasMaxLength(50)
                .IsRequired();

            colorBuilder.Property(c => c.HexCode)
                .HasColumnName("color_hex")
                .HasMaxLength(7)
                .IsRequired();
        });

        builder.HasIndex(f => f.RetreatId);

        builder.Navigation(f => f.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        
        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(f => f.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);
    
        builder.Property(f => f.GroupLink)
            .HasColumnName("group_link")
            .HasMaxLength(512);

        builder.Property(f => f.GroupExternalId)
            .HasColumnName("group_external_id")
            .HasMaxLength(128);

        builder.Property(f => f.GroupCreatedAt)
            .HasColumnName("group_created_at");

        builder.Property(f => f.GroupChannel)
            .HasColumnName("group_channel")
            .HasMaxLength(16);

        builder.Property(f => f.GroupLastNotifiedAt)
            .HasColumnName("group_last_notified_at");

        builder.Property(f => f.GroupStatus)
            .HasColumnName("group_status")
            .HasConversion<int>()
            .HasDefaultValue(GroupStatus.None)
            .IsRequired();

        builder.Property(f => f.GroupVersion)
            .HasColumnName("group_version")
            .HasDefaultValue(0)
            .IsRequired();
        
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_families_capacity_positive", "capacity >= 4");
        });
        
        builder.HasIndex(f => new { f.RetreatId, f.Name }).IsUnique();
        
        builder.HasIndex(f => new { f.RetreatId, f.GroupStatus });
    }
}
