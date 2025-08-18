using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Infrastructure.Persistence.Configurations;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> b)
    {
        b.ToTable("notification_messages");

        b.HasKey(x => x.Id);
        b.Property(x => x.Channel).IsRequired();
        b.Property(x => x.Status).IsRequired();

        b.Property(x => x.RecipientEmail).HasMaxLength(320);
        b.Property(x => x.RecipientPhone).HasMaxLength(32);
        b.Property(x => x.RecipientName).HasMaxLength(200);

        b.Property(x => x.TemplateKey).HasMaxLength(100);
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Body);

        b.Property(x => x.ExternalCorrelationId).HasMaxLength(100);

        b.HasIndex(x => x.ExternalCorrelationId).HasDatabaseName("ix_notification_extcorr");
        b.HasIndex(x => x.RegistrationId).HasDatabaseName("ix_notification_registration");
        b.HasIndex(x => x.RetreatId).HasDatabaseName("ix_notification_retreat");
    }
}