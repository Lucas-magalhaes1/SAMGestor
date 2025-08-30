namespace SAMGestor.Contracts;

public sealed record PaymentConfirmedV1(
    Guid PaymentId,
    Guid RegistrationId,
    Guid RetreatId,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt
);