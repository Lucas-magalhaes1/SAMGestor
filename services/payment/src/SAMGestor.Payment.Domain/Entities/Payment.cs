using SAMGestor.Payment.Domain.Enums;

namespace SAMGestor.Payment.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid RegistrationId { get; private set; }
    public Guid RetreatId { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";

    public string Provider { get; private set; } = "mercadopago";
    public string? ProviderPreferenceId { get; private set; } // id da preference / intent
    public string? ProviderPaymentId { get; private set; }    // id do pagamento aprovado
    public string? LinkUrl { get; private set; }

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private Payment() { } // EF

    public Payment(Guid registrationId, Guid retreatId, decimal amount, string currency = "BRL")
    {
        RegistrationId = registrationId;
        RetreatId = retreatId;
        Amount = amount;
        Currency = currency;
    }
    
    public void SetProvider(string provider)
    {
        Provider = provider;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLink(string linkUrl, string? preferenceId, DateTimeOffset? expiresAt = null)
    {
        LinkUrl = linkUrl;
        ProviderPreferenceId = preferenceId;
        ExpiresAt = expiresAt;
        Status = PaymentStatus.LinkCreated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(string providerPaymentId, DateTimeOffset paidAt)
    {
        ProviderPaymentId = providerPaymentId;
        PaidAt = paidAt;
        Status = PaymentStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()  { Status = PaymentStatus.Failed;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkExpired() { Status = PaymentStatus.Expired; UpdatedAt = DateTimeOffset.UtcNow; }
}
