using MediatR;

namespace SAMGestor.Application.Features.Families.Lock;

public sealed record LockSingleFamilyCommand(Guid RetreatId, Guid FamilyId, bool Lock)
    : IRequest<LockSingleFamilyResponse>;

public sealed record LockSingleFamilyResponse(Guid FamilyId, bool Locked, int Version);