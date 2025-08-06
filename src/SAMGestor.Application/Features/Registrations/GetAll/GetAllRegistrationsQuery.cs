using MediatR;

namespace SAMGestor.Application.Features.Registrations.GetAll;

public record GetAllRegistrationsQuery(
    Guid retreatId,
    string? status = null,
    string? region = null,
    int skip = 0,
    int take = 20) : IRequest<GetAllRegistrationsResponse>;