using MediatR;
using System;

namespace SAMGestor.Application.Features.Registrations.GetById;

public sealed record GetRegistrationByIdQuery(Guid RegistrationId)
    : IRequest<GetRegistrationByIdResponse?>;