using MediatR;

namespace SAMGestor.Application.Features.Service.Registrations.Confirmed;

public sealed record GetConfirmedServiceRegistrationsQuery(Guid RetreatId)
    : IRequest<IReadOnlyList<GetConfirmedServiceRegistrationsResponse>>;