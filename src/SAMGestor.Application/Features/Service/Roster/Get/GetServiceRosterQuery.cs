using MediatR;

namespace SAMGestor.Application.Features.Service.Roster.Get;

public sealed record GetServiceRosterQuery(Guid RetreatId)
    : IRequest<GetServiceRosterResponse>;