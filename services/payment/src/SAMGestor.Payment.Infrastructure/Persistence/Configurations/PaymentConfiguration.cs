using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = SAMGestor.Payment.Domain.Entities.Payment;

namespace SAMGestor.Payment.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> b)
    {
        b.ToTable("payments");

        b.HasKey(p => p.Id);

        b.Property(p => p.Amount)
            .HasColumnType("numeric(14,2)")
            .IsRequired();

        b.Property(p => p.Currency)
            .HasMaxLength(8)
            .IsRequired();

        b.Property(p => p.Provider)
            .HasMaxLength(40)
            .IsRequired();

        b.Property(p => p.ProviderPreferenceId).HasMaxLength(100);
        b.Property(p => p.ProviderPaymentId).HasMaxLength(100);
        b.Property(p => p.LinkUrl).HasMaxLength(500);

        b.Property(p => p.Status).IsRequired();

        // Índices
        b.HasIndex(p => p.RegistrationId).IsUnique(); // idempotência: 1 payment por inscrição
        b.HasIndex(p => p.ProviderPaymentId);
    }
}