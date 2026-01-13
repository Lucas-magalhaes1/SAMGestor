using MediatR;

namespace SAMGestor.Application.Features.Families.UpdateGodparents;

public sealed record UpdateGodparentsCommand(
    Guid RetreatId,
    Guid FamilyId,
    IReadOnlyList<Guid> PadrinhoIds,   
    IReadOnlyList<Guid> MadrinhaIds     
) : IRequest<UpdateGodparentsResult>;