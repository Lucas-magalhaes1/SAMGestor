using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;
public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(p => p.Value).HasColumnName("name").HasMaxLength(120).IsRequired();
        });

        builder.OwnsOne(r => r.Cpf, c =>
        {
            c.Property(p => p.Value).HasColumnName("cpf").HasMaxLength(11).IsRequired();
            c.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(r => r.Email, e =>
        {
            e.Property(p => p.Value).HasColumnName("email").HasMaxLength(160).IsRequired();
            e.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(r => r.PhotoUrl, u =>
        {
            u.Property(p => p.Value).HasColumnName("photo_url").HasMaxLength(300);
        });
        
        builder.Property(r => r.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(r => r.BirthDate).HasColumnName("birth_date").IsRequired();
        builder.Property(r => r.City).HasColumnName("city").HasMaxLength(80).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(r => r.ParticipationCategory).HasColumnName("participation_category").HasConversion<string>().IsRequired();
        builder.Property(r => r.Enabled).HasColumnName("enabled").IsRequired();
        builder.Property(r => r.Region).HasColumnName("region").HasMaxLength(60).IsRequired();
        builder.Property(r => r.CompletedRetreat).HasColumnName("completed_retreat").IsRequired();
        builder.Property(r => r.RegistrationDate).HasColumnName("registration_date").IsRequired();
    }
}