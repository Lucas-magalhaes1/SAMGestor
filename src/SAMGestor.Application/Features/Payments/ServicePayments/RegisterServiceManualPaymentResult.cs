namespace SAMGestor.Application.Features.Payments.ServicePayments;

public sealed record RegisterServiceManualPaymentResult(
    Guid ProofId,
    Guid ServiceRegistrationId,
    string StorageKey,
    string Status
);