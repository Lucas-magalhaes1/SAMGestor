using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class ManualPaymentProof : Entity<Guid>
{
    public Guid RegistrationId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTime PaymentDate { get; private set; }
    
    public string ProofStorageKey { get; private set; }
    public string? ProofContentType { get; private set; }
    public int? ProofSizeBytes { get; private set; }
    public DateTime ProofUploadedAt { get; private set; }
    
    public string? Notes { get; private set; }
    public Guid RegisteredByUserId { get; private set; } 
    public DateTime RegisteredAt { get; private set; }

    private ManualPaymentProof() { }

    public ManualPaymentProof(
        Guid registrationId,
        Money amount,
        PaymentMethod method,
        DateTime paymentDate,
        string proofStorageKey,
        string? proofContentType,
        int? proofSizeBytes,
        string? notes,
        Guid registeredByUserId)
    {
        Id = Guid.NewGuid();
        RegistrationId = registrationId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Method = method;
        PaymentDate = paymentDate;
        ProofStorageKey = proofStorageKey ?? throw new ArgumentNullException(nameof(proofStorageKey));
        ProofContentType = proofContentType;
        ProofSizeBytes = proofSizeBytes;
        ProofUploadedAt = DateTime.UtcNow;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RegisteredByUserId = registeredByUserId;
        RegisteredAt = DateTime.UtcNow;
    }
}