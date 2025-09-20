namespace SAMGestor.Contracts;

public sealed record FamilyGroupCreatedV1(
    Guid RetreatId,
    Guid FamilyId,
    string Channel,
    string Link,
    string? ExternalId,
    DateTimeOffset CreatedAt
);