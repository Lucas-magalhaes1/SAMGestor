using System;

namespace SAMGestor.Application.Features.Families.Create;

public sealed record CreateFamilyResult(
    bool Created,
    Guid? FamilyId,
    int Version,
    IReadOnlyList<CreateFamilyWarningDto> Warnings
);

public sealed record CreateFamilyWarningDto(
    string Severity,            
    string Code,                
    string Message,
    IReadOnlyList<Guid> RegistrationIds
);