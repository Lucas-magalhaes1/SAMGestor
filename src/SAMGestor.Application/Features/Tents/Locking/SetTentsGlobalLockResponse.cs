namespace SAMGestor.Application.Features.Tents.Locking;

public sealed record SetTentsGlobalLockResponse(
    Guid RetreatId,
    bool Locked,
    int  Version
);