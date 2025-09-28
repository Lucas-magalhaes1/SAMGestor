namespace SAMGestor.Domain.Entities;

public class ServiceRegistrationPayment
{
    public Guid ServiceRegistrationId { get; private set; }
    public Guid PaymentId             { get; private set; }
    public DateTime CreatedAt         { get; private set; }

    private ServiceRegistrationPayment() { }

    public ServiceRegistrationPayment(Guid serviceRegistrationId, Guid paymentId)
    {
        ServiceRegistrationId = serviceRegistrationId;
        PaymentId             = paymentId;
        CreatedAt             = DateTime.UtcNow;
    }
}