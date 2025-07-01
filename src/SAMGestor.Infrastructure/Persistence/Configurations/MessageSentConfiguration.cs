using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class MessageSentConfiguration : IEntityTypeConfiguration<MessageSent>
{
    public void Configure(EntityTypeBuilder<MessageSent> builder)
    {
        builder.ToTable("messages_sent");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RegistrationId).HasColumnName("registration_id").IsRequired();
        builder.Property(m => m.MessageTemplateId).HasColumnName("message_template_id").IsRequired();
        builder.Property(m => m.SentAt).HasColumnName("sent_at").IsRequired();
        builder.Property(m => m.Status).HasColumnName("status").HasConversion<string>().IsRequired();
    }
}