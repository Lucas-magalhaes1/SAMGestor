using MediatR;
using SAMGestor.Application.Dtos;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.PublicList;

public sealed class PublicListServiceSpacesHandler(
    IServiceSpaceRepository repo
) : IRequestHandler<PublicListServiceSpacesQuery, IReadOnlyList<ServiceSpacePublicDto>>
{
    public async Task<IReadOnlyList<ServiceSpacePublicDto>> Handle(
        PublicListServiceSpacesQuery request, CancellationToken ct)
    {
        var spaces = await repo.ListActiveByRetreatAsync(request.RetreatId, ct);

        return spaces
            .OrderBy(s => s.Name)
            .Select(s => new ServiceSpacePublicDto(s.Id, s.Name, s.Description))
            .ToArray();
    }
}