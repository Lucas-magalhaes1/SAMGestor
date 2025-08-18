using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Infrastructure.Persistence.Configurations;

public class NotificationDispatchLogConfiguration : IEntityTypeConfiguration<NotificationDispatchLog>
{
    public void Configure(EntityTypeBuilder<NotificationDispatchLog> b)
    {
        b.ToTable("notification_dispatch_logs");

        b.HasKey(x => x.Id);
        b.Property(x => x.NotificationMessageId).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.Error);

        b.HasIndex(x => x.NotificationMessageId).HasDatabaseName("ix_dispatch_message");
    }
}