using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Registrations.GetAll;

public record GetAllRegistrationsQuery(
    Guid   retreatId,
    string? status   = null,   

    // filtros opcionais
    Gender? gender   = null,
    int?    minAge   = null,
    int?    maxAge   = null,
    string? city     = null,
    UF?     state    = null,
    string? search   = null, 
    bool?   hasPhoto = null,  

    // paginação
    int skip = 0,
    int take = 20
) : IRequest<GetAllRegistrationsResponse>;