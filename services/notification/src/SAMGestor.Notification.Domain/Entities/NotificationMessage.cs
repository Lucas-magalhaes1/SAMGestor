using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Domain.Entities;

public class NotificationMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    // Destinatário
    public string? RecipientName { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? RecipientPhone { get; private set; }

    // Conteúdo
    public string? TemplateKey { get; private set; } // ex.: "participant-selected"
    public string? Subject { get; private set; }
    public string? Body { get; private set; }

    // Identificadores de correlação (útil p/ idempotência e auditoria)
    public Guid? RegistrationId { get; private set; }
    public Guid? RetreatId { get; private set; }
    public string? ExternalCorrelationId { get; private set; } // opcional

    // Auditoria
    public int Attempts { get; private set; } = 0;
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; private set; }

    // Navegação
    public List<NotificationDispatchLog> DispatchLogs { get; } = new();

    private NotificationMessage() { } // EF

    public NotificationMessage(
        NotificationChannel channel,
        string? recipientName,
        string? recipientEmail,
        string? recipientPhone,
        string? templateKey,
        string? subject,
        string? body,
        Guid? registrationId,
        Guid? retreatId,
        string? externalCorrelationId)
    {
        Channel = channel;
        RecipientName = recipientName;
        RecipientEmail = recipientEmail;
        RecipientPhone = recipientPhone;
        TemplateKey = templateKey;
        Subject = subject;
        Body = body;
        RegistrationId = registrationId;
        RetreatId = retreatId;
        ExternalCorrelationId = externalCorrelationId;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.Failed;
        LastError = error;
    }

    public void IncrementAttempt() => Attempts++;
}
