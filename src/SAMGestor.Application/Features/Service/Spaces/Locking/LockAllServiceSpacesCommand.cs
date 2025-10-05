using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Locking;

public sealed record LockAllServiceSpacesCommand(
    Guid RetreatId,
    bool Lock
) : IRequest<LockAllServiceSpacesResponse>;

public sealed record LockAllServiceSpacesResponse(int Version, int ChangedCount);