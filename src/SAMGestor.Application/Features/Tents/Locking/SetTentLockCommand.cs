using MediatR;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed record SetTentLockCommand(
    Guid RetreatId,
    Guid TentId,
    bool Lock
) : IRequest<SetTentLockResponse>;