namespace SAMGestor.Application.Features.Families.Generate;

public sealed record GenerateFamiliesResponse(
    int Version,
    IReadOnlyList<GeneratedFamilyDto> Families
);

public sealed record GeneratedFamilyDto(
    Guid FamilyId,
    string Name,
    string ColorName,                        
    string ColorHex,                         
    int Capacity,
    int TotalMembers,
    int MaleCount,
    int FemaleCount,
    int Remaining,
    IReadOnlyList<GeneratedMemberDto> Members,
    IReadOnlyList<FamilyAlertDto> Alerts
);

public sealed record GeneratedMemberDto(
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

public sealed record FamilyAlertDto(
    string Severity,   
    string Code,      
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);