
using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Entities;

public class MessageSent : Entity<Guid>
{
    public Guid RegistrationId { get; private set; }
    public Guid MessageTemplateId { get; private set; }
    public DateTime SentAt { get; private set; }
    public DeliveryStatus Status { get; private set; }

    private MessageSent() { }

    public MessageSent(Guid registrationId, Guid messageTemplateId, DeliveryStatus status)
    {
        Id = Guid.NewGuid();
        RegistrationId = registrationId;
        MessageTemplateId = messageTemplateId;
        SentAt = DateTime.UtcNow;
        Status = status;
    }

    public void MarkSuccess() => Status = DeliveryStatus.Success;
    public void MarkFailure() => Status = DeliveryStatus.Failure;
}