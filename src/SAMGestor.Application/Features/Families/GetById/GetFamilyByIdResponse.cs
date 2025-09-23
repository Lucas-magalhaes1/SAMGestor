// SAMGestor.Application/Features/Families/GetById/GetFamilyByIdResponse.cs
namespace SAMGestor.Application.Features.Families.GetById;

public sealed record GetFamilyByIdResponse(
    int Version,
    FamilyDto? Family
);

public sealed record FamilyDto(
    Guid FamilyId,
    string Name,
    int Capacity,
    int TotalMembers,
    int MaleCount,
    int FemaleCount,
    int Remaining,
    bool IsLocked,
    
    string? GroupStatus,              
    string? GroupLink,
    string? GroupExternalId,
    string? GroupChannel,              
    DateTimeOffset? GroupCreatedAt,
    DateTimeOffset? GroupLastNotifiedAt,
    int GroupVersion,
    
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