using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages", schema: "core");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(200).IsRequired();
        b.Property(x => x.Source).HasMaxLength(100).IsRequired();
        b.Property(x => x.TraceId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Data).IsRequired();
        b.Property(x => x.Attempts).HasDefaultValue(0);

        b.HasIndex(x => x.ProcessedAt).HasDatabaseName("ix_outbox_processed");
        b.HasIndex(x => new { x.Type, x.CreatedAt }).HasDatabaseName("ix_outbox_type_created");
    }
}