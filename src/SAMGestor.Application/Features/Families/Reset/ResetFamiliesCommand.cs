using MediatR;

namespace SAMGestor.Application.Features.Families.Reset;

public sealed record ResetFamiliesCommand(Guid RetreatId, bool ForceLockedFamilies = false)
    : IRequest<ResetFamiliesResponse>;

public sealed record ResetFamiliesResponse(
    int Version,
    int FamiliesDeleted,
    int MembersDeleted
);