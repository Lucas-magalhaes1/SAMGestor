using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Locking;

public sealed record LockServiceSpaceCommand(
    Guid RetreatId,
    Guid SpaceId,
    bool Lock
) : IRequest<LockServiceSpaceResponse>;

public sealed record LockServiceSpaceResponse(int Version, bool Changed);