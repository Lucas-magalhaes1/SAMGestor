namespace SAMGestor.Contracts;

public sealed record PasswordResetRequestedV1(
    Guid UserId,
    string Name,
    string Email,
    string ResetToken,
    string ResetUrl,
    DateTimeOffset ExpiresAt
);