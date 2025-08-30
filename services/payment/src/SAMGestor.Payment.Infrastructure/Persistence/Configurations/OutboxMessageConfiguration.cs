using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Payment.Infrastructure.Messaging.Outbox;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages", PaymentDbContext.Schema);

        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(200).IsRequired();
        b.Property(x => x.Source).HasMaxLength(100).IsRequired();
        b.Property(x => x.TraceId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Data).HasColumnType("jsonb").IsRequired(); // ou remova HasColumnType para 'text'

        b.HasIndex(x => x.ProcessedAt).HasDatabaseName("ix_outbox_processed");
        b.HasIndex(x => new { x.Type, x.CreatedAt }).HasDatabaseName("ix_outbox_type_created");
    }
}