using MediatR;

namespace SAMGestor.Application.Features.Tents.TentRoster.Unassign;

public sealed record UnassignFromTentCommand(
    Guid RetreatId,
    IReadOnlyList<Guid> RegistrationIds
) : IRequest<UnassignFromTentResponse>;

public sealed record UnassignFromTentResponse(
    int Version,
    int Removed,
    IReadOnlyList<Guid> AffectedTentIds
);