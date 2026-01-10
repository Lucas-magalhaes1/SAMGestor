namespace SAMGestor.Application.Features.Payments;

public sealed record RegisterManualPaymentResult(
    Guid ProofId,
    Guid RegistrationId,
    string StorageKey,
    string Status
);