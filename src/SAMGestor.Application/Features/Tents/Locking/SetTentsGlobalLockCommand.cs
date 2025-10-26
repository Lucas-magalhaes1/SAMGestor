using MediatR;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed record SetTentsGlobalLockCommand(
    Guid RetreatId,
    bool Lock
) : IRequest<SetTentsGlobalLockResponse>;