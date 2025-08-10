using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;
public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(r => r.Id);
        
        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(p => p.Value)
             .HasColumnName("name")
             .HasMaxLength(120)
             .IsRequired();
        });
        
        builder.Property(r => r.Cpf)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new CPF(fromProvider)
            )
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .IsRequired();
        
        builder.Property(r => r.Email)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new EmailAddress(fromProvider)
            )
            .HasColumnName("email")
            .HasMaxLength(160)
            .IsRequired();
        
        builder.OwnsOne(r => r.PhotoUrl, u =>
        {
            u.Property(p => p.Value)
             .HasColumnName("photo_url")
             .HasMaxLength(300);
        });

        builder.Property(r => r.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(r => r.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(r => r.BirthDate).HasColumnName("birth_date").IsRequired();
        builder.Property(r => r.City).HasColumnName("city").HasMaxLength(80).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(r => r.ParticipationCategory).HasColumnName("participation_category").HasConversion<string>().IsRequired();
        builder.Property(r => r.Enabled).HasColumnName("enabled").IsRequired();
        builder.Property(r => r.Region).HasColumnName("region").HasMaxLength(60).IsRequired();
        builder.Property(r => r.CompletedRetreat).HasColumnName("completed_retreat").IsRequired();
        builder.Property(r => r.RegistrationDate).HasColumnName("registration_date").IsRequired();
        
        builder.HasIndex(r => new { r.RetreatId, r.Status, r.Gender });
        
        builder.HasIndex(r => new { r.RetreatId, r.Cpf }).IsUnique();
        builder.HasIndex(r => new { r.RetreatId, r.Email }).IsUnique();
        
         builder.HasIndex(r => r.Cpf);
         builder.HasIndex(r => r.Email);
    }
}
