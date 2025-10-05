using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.PublicList;

public sealed class PublicListServiceSpacesHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo
) : IRequestHandler<PublicListServiceSpacesQuery, PublicListServiceSpacesResponse>
{
    public async Task<PublicListServiceSpacesResponse> Handle(PublicListServiceSpacesQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListActiveByRetreatAsync(q.RetreatId, ct);

        var items = spaces
            .OrderBy(s => s.Name)
            .Select(s => new PublicItem(s.Id, s.Name, s.Description))
            .ToList();

        return new PublicListServiceSpacesResponse(retreat.ServiceSpacesVersion, items);
    }
}