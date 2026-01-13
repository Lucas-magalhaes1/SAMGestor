using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("family_members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RetreatId)
               .HasColumnName("retreat_id")
               .IsRequired();

        builder.Property(m => m.FamilyId)
               .HasColumnName("family_id")
               .IsRequired();

        builder.Property(m => m.RegistrationId)
               .HasColumnName("registration_id")
               .IsRequired();

        builder.Property(m => m.Position)
               .HasColumnName("position")
               .IsRequired();
        
        builder.Property(m => m.IsPadrinho)
               .HasColumnName("is_padrinho")
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(m => m.IsMadrinha)
               .HasColumnName("is_madrinha")
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(m => m.AssignedAt)
               .HasColumnName("assigned_at")
               .HasColumnType("timestamptz")   
               .HasDefaultValueSql("now()")    
               .IsRequired();

        builder.HasOne<Family>()
               .WithMany(f => f.Members)
               .HasForeignKey(m => m.FamilyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Registration>()
               .WithMany()
               .HasForeignKey(m => m.RegistrationId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(m => new { m.RetreatId, m.RegistrationId }).IsUnique(); 
        builder.HasIndex(m => new { m.RetreatId, m.FamilyId });                  

        builder.HasIndex(m => new { m.FamilyId, m.Position }).IsUnique();       
        builder.HasIndex(m => new { m.FamilyId, m.RegistrationId }).IsUnique();
        
        builder.HasIndex(m => new { m.FamilyId, m.IsPadrinho });
        builder.HasIndex(m => new { m.FamilyId, m.IsMadrinha });
        
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_family_members_position_nonneg", "position >= 0");
            
            t.HasCheckConstraint("ck_family_members_godparent_exclusive", 
                "NOT (is_padrinho = true AND is_madrinha = true)");
        });
    }
}
