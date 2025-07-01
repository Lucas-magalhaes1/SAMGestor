using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ChangeLogConfiguration : IEntityTypeConfiguration<ChangeLog>
{
    public void Configure(EntityTypeBuilder<ChangeLog> builder)
    {
        builder.ToTable("change_logs");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.EntityName).HasColumnName("entity_name").HasMaxLength(120).IsRequired();
        builder.Property(c => c.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
    }
}