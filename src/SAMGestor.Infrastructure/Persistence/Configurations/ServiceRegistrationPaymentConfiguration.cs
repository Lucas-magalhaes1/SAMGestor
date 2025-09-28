using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ServiceRegistrationPaymentConfiguration : IEntityTypeConfiguration<ServiceRegistrationPayment>
{
    public void Configure(EntityTypeBuilder<ServiceRegistrationPayment> builder)
    {
        builder.ToTable("service_registration_payments");

        builder.HasKey(x => new { x.ServiceRegistrationId, x.PaymentId });

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<ServiceRegistration>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => x.PaymentId).IsUnique();
    }
}