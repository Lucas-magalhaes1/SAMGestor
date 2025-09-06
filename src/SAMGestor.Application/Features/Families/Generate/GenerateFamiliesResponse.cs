using System;
using System.Collections.Generic;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed record GenerateFamiliesResponse(
    int Version,
    IReadOnlyList<GeneratedFamilyDto> Families
);

public sealed record GeneratedFamilyDto(
    Guid FamilyId,
    string Name,
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