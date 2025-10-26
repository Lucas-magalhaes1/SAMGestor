using SAMGestor.Application.Features.Tents.TentRoster.Assign;

namespace SAMGestor.Application.Features.Tents.TentRoster.AutoAssign;

public sealed record AutoAssignTentsResponse(
    int Version,
    IReadOnlyList<TentRosterSpaceView> Tents
);