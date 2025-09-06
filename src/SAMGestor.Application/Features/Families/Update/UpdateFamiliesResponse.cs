namespace SAMGestor.Application.Features.Families.Update;

public sealed record UpdateFamiliesResponse(
    int Version,
    IReadOnlyList<FamilyDto> Families,
    IReadOnlyList<FamilyErrorDto> Errors,
    IReadOnlyList<FamilyAlertDto> Warnings
);

public sealed record FamilyDto(
    Guid FamilyId,
    string Name,
    int Capacity,
    int TotalMembers,
    int MaleCount,
    int FemaleCount,
    int Remaining,
    IReadOnlyList<MemberDto> Members,
    IReadOnlyList<FamilyAlertDto> Alerts
);

public sealed record MemberDto(
    Guid RegistrationId,
    string Name,
    string Gender,
    string City,
    int Position
);

public sealed record FamilyErrorDto(
    string Code,                 // e.g. "CAPACITY_EXCEEDED", "COMPOSITION_INVALID", "RELATIONSHIP_CONFLICT", "SAME_SURNAME", "DUPLICATE_MEMBER", "VERSION_MISMATCH"
    string Message,
    Guid? FamilyId,
    IReadOnlyList<Guid> RegistrationIds
);

public sealed record FamilyAlertDto(
    string Severity,             // "critical" | "warning"
    string Code,                 // e.g. "SAME_CITY", "GENDER_DEVIATION"
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);