namespace SAMGestor.Contracts;

public sealed record FamilyGroupCreateFailedV1(
    Guid RetreatId,
    Guid FamilyId,
    string Channel,
    string Reason,
    IReadOnlyList<Guid> AffectedRegistrationIds
);