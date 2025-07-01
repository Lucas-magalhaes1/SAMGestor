using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Payment : Entity<Guid>
{
    public Guid RegistrationId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Payment() { }

    public Payment(Guid registrationId, Money amount, PaymentMethod method)
    {
        Id = Guid.NewGuid();
        RegistrationId = registrationId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
    }

    public void MarkAsPaid()
    {
        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    public void Cancel() => Status = PaymentStatus.Canceled;
}