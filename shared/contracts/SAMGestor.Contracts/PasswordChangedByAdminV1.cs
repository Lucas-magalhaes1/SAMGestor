public sealed record PasswordChangedByAdminV1(
    Guid UserId,
    string Name,
    string Email,
    string ChangedBy
);