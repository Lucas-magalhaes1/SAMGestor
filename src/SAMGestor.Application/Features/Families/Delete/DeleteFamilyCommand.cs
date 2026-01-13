using MediatR;

namespace SAMGestor.Application.Features.Families.Delete;

public sealed record DeleteFamilyCommand(
    Guid RetreatId, 
    Guid FamilyId
) : IRequest<DeleteFamilyResponse>;

public sealed record DeleteFamilyResponse(
    int Version,
    string FamilyName,
    int MembersDeleted
);