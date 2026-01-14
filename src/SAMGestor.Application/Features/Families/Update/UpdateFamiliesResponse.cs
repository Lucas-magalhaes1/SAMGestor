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
    string ColorName,                       
    string ColorHex,                       
    int Capacity,
    int TotalMembers,
    int MaleCount,
    int FemaleCount,
    decimal MalePercentage,                 
    decimal FemalePercentage,             
    int Remaining,
    bool IsLocked,                          
    IReadOnlyList<MemberDto> Members,
    IReadOnlyList<FamilyAlertDto> Alerts
);

public sealed record MemberDto(
    Guid RegistrationId,
    string Name,
    string Email,                           
    string Phone,                          
    string Gender,
    string City,
    int Position,
    bool IsPadrinho,                       
    bool IsMadrinha                     
);

public sealed record FamilyErrorDto(
    string Code,
    string Message,
    Guid? FamilyId,
    IReadOnlyList<Guid> RegistrationIds
);

public sealed record FamilyAlertDto(
    string Severity,
    string Code,
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);