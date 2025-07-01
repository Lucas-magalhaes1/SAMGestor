using SAMGestor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RegistrationId).HasColumnName("registration_id").IsRequired();

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(v => v.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(v => v.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(p => p.Method).HasColumnName("method").HasConversion<string>().IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(p => p.PaidAt).HasColumnName("paid_at");
    }
}