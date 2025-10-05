using MediatR;

namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed record GetServiceRegistrationQuery(
    Guid RetreatId,
    Guid RegistrationId
) : IRequest<GetServiceRegistrationResponse>;