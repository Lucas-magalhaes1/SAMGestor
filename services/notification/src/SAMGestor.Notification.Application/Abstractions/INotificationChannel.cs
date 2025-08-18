using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Application.Abstractions;

public interface INotificationChannel
{
    string Name { get; } // "email", "whatsapp", ...

    Task SendAsync(NotificationMessage message, CancellationToken ct);
}