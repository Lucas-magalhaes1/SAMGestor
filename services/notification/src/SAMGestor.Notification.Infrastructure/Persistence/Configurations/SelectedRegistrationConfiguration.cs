using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums; 

namespace SAMGestor.Notification.Infrastructure.Persistence.Configurations;

public sealed class SelectedRegistrationConfiguration : IEntityTypeConfiguration<SelectedRegistration>
{
    public void Configure(EntityTypeBuilder<SelectedRegistration> b)
    {
        b.ToTable("selected_registrations");

        b.HasKey(x => x.RegistrationId);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Amount).HasColumnType("numeric(14,2)").IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();

        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        
        b.Property(x => x.Kind)
            .HasConversion<int>()
            .HasDefaultValue(SelectionKind.Selection)
            .IsRequired();

        b.HasIndex(x => x.RetreatId);
    }
}