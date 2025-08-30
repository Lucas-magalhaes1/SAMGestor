namespace SAMGestor.Contracts;

public sealed record PaymentRequestedV1(
    Guid RegistrationId,
    Guid RetreatId,
    decimal Amount,
    string Currency,
    string Name,
    string Email,
    string? Phone
);