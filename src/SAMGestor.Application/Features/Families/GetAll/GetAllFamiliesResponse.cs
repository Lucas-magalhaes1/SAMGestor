namespace SAMGestor.Application.Features.Families.GetAll;

public sealed record GetAllFamiliesResponse(
    int Version,
    bool FamiliesLocked,  
    IReadOnlyList<FamilyDto> Families
);

public sealed record FamilyDto(
    Guid FamilyId,
    string Name,
    string ColorName,                    
    string ColorHex,                     
    bool IsLocked,                     
    int Capacity,
    int TotalMembers,
    int MaleCount,
    int FemaleCount,
    decimal MalePercentage,             
    decimal FemalePercentage,          
    int Remaining,
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

public sealed record FamilyAlertDto(
    string Severity,   
    string Code,       
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);