namespace SAMGestor.Application.Features.Tents.Locking;

public sealed record SetTentLockResponse(
    Guid RetreatId,
    Guid TentId,
    bool Locked,
    int  Version
);