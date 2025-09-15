using MediatR;

namespace SAMGestor.Application.Features.Families.Lock;

public sealed record LockFamiliesCommand(Guid RetreatId, bool Lock)
    : IRequest<LockFamiliesResponse>;

public sealed record LockFamiliesResponse(int Version, bool Locked);