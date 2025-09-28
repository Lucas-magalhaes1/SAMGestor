using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ServiceRegistrationConfiguration : IEntityTypeConfiguration<ServiceRegistration>
{
    public void Configure(EntityTypeBuilder<ServiceRegistration> builder)
    {
        builder.ToTable("service_registrations");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.HasOne<Retreat>()             
            .WithMany()
            .HasForeignKey(x => x.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.OwnsOne(x => x.Name, fn =>
        {
            fn.Property(p => p.Value)
              .HasColumnName("full_name")
              .HasMaxLength(160)
              .IsRequired();
        });

        builder.Property(x => x.Cpf)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new CPF(fromProvider)
            )
            .HasColumnName("cpf")
            .HasMaxLength(11) 
            .IsRequired();

        builder.Property(x => x.Email)
            .HasConversion(
                toProvider => toProvider.Value,
                fromProvider => new EmailAddress(fromProvider)
            )
            .HasColumnName("email")
            .HasMaxLength(160)
            .IsRequired();

        builder.OwnsOne(x => x.PhotoUrl, p =>
        {
            p.Property(u => u.Value)
             .HasColumnName("photo_url")
             .HasMaxLength(512);
        });
        
        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()  
            .IsRequired();

        builder.Property(x => x.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Region)
            .HasColumnName("region")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()   
            .IsRequired();

        builder.Property(x => x.Enabled)
            .HasColumnName("enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.RegistrationDate)
            .HasColumnName("registration_date")
            .IsRequired();

        builder.Property(x => x.PreferredSpaceId)
            .HasColumnName("preferred_space_id");

        builder.HasOne<ServiceSpace>()
            .WithMany()
            .HasForeignKey(x => x.PreferredSpaceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => new { e.RetreatId, e.Cpf }).IsUnique();
        builder.HasIndex(e => new { e.RetreatId, e.Email }).IsUnique();
        builder.HasIndex(e => new { e.RetreatId, e.Status });
        builder.HasIndex(e => e.PreferredSpaceId);
        builder.HasIndex(e => e.Cpf);
        builder.HasIndex(e => e.Email);
    }
}
