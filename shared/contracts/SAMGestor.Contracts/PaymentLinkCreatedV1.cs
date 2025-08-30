namespace SAMGestor.Contracts;

public sealed record PaymentLinkCreatedV1(
    Guid PaymentId,
    Guid RegistrationId,
    Guid RetreatId,
    decimal Amount,
    string Currency,
    string LinkUrl,
    DateTimeOffset? ExpiresAt
);