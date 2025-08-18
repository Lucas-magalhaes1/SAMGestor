using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Domain.Entities;

public class NotificationDispatchLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid NotificationMessageId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private NotificationDispatchLog() { } // EF

    public NotificationDispatchLog(Guid messageId, NotificationStatus status, string? error)
    {
        NotificationMessageId = messageId;
        Status = status;
        Error = error;
    }
}