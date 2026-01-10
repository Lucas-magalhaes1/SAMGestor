using SAMGestor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ManualPaymentProofConfiguration : IEntityTypeConfiguration<ManualPaymentProof>
{
    public void Configure(EntityTypeBuilder<ManualPaymentProof> builder)
    {
        builder.ToTable("manual_payment_proofs");
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(v => v.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            
            m.Property(v => v.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(p => p.ProofStorageKey)
            .HasColumnName("proof_storage_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.ProofContentType)
            .HasColumnName("proof_content_type")
            .HasMaxLength(100);

        builder.Property(p => p.ProofSizeBytes)
            .HasColumnName("proof_size_bytes");

        builder.Property(p => p.ProofUploadedAt)
            .HasColumnName("proof_uploaded_at")
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(p => p.RegisteredByUserId)
            .HasColumnName("registered_by_user_id")
            .IsRequired();

        builder.Property(p => p.RegisteredAt)
            .HasColumnName("registered_at")
            .IsRequired();
        
        builder.HasIndex(p => p.RegistrationId)
            .IsUnique()
            .HasDatabaseName("ix_manual_payment_proofs_registration_id");

        builder.HasIndex(p => p.RegisteredAt)
            .HasDatabaseName("ix_manual_payment_proofs_registered_at");
        
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(p => p.RegistrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
