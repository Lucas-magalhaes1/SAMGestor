using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.ToTable("message_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasColumnName("type").HasConversion<string>().IsRequired();
        builder.Property(t => t.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
        builder.Property(t => t.HasPlaceholders).HasColumnName("has_placeholders").IsRequired();
    }
}