using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Delete;

public sealed record DeleteServiceSpaceCommand(Guid RetreatId, Guid SpaceId)
    : IRequest<DeleteServiceSpaceResponse>;

public sealed record DeleteServiceSpaceResponse(bool Deleted, int Version);