using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(NotificationMessage message, CancellationToken ct);
    Task UpdateAsync(NotificationMessage message, CancellationToken ct);
    Task AddLogAsync(NotificationDispatchLog log, CancellationToken ct);
}