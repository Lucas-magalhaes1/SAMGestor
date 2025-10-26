using MediatR;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;

namespace SAMGestor.Application.Features.Tents.TentRoster.Get;

public sealed record GetTentRosterQuery(Guid RetreatId) : IRequest<GetTentRosterResponse>;

public sealed record GetTentRosterResponse(
    int Version,
    List<TentRosterSpaceView> Tents
);