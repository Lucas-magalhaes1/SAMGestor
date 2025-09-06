namespace SAMGestor.Application.Features.Families.GetAll;

public sealed record GetAllFamiliesResponse(
    int Version,
    IReadOnlyList<FamilyDto> Families
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

public sealed record FamilyAlertDto(
    string Severity,   
    string Code,       
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);