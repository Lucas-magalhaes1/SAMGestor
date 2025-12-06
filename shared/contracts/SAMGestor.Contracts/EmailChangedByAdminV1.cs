namespace SAMGestor.Contracts;

public sealed record EmailChangedByAdminV1(
    Guid UserId,
    string Name,
    string NewEmail,
    string ConfirmUrl
);