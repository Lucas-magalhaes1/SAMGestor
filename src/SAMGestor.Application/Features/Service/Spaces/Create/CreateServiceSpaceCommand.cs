using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Create;

public sealed record CreateServiceSpaceCommand(
    Guid   RetreatId,
    string Name,
    string? Description,
    int    MinPeople,
    int    MaxPeople,
    bool   IsActive
) : IRequest<CreateServiceSpaceResponse>;

public sealed record CreateServiceSpaceResponse(Guid SpaceId, int Version);