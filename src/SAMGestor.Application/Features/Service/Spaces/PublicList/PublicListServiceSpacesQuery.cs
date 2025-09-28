using MediatR;
using SAMGestor.Application.Dtos;

namespace SAMGestor.Application.Features.Service.Spaces.PublicList;

public sealed record PublicListServiceSpacesQuery(Guid RetreatId)
    : IRequest<IReadOnlyList<ServiceSpacePublicDto>>;