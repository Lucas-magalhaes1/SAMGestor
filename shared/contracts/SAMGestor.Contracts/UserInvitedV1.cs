namespace SAMGestor.Contracts;

public sealed record UserInvitedV1(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string ConfirmationToken,
    string ConfirmUrl,
    DateTimeOffset ExpiresAt,
    string CreatedBy
);