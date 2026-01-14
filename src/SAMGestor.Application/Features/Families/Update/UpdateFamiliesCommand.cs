using MediatR;

namespace SAMGestor.Application.Features.Families.Update;

public sealed record UpdateFamiliesCommand(
    Guid RetreatId,
    int Version,
    IReadOnlyList<UpdateFamilyDto> Families,
    bool IgnoreWarnings = false
) : IRequest<UpdateFamiliesResponse>;

public sealed record UpdateFamilyDto(
    Guid FamilyId,
    string Name,                            
    string ColorName,                      
    int Capacity,                          
    IReadOnlyList<UpdateMemberDto> Members,
    IReadOnlyList<Guid> PadrinhoIds,      
    IReadOnlyList<Guid> MadrinhaIds      
);

public sealed record UpdateMemberDto(
    Guid RegistrationId,
    int Position
);