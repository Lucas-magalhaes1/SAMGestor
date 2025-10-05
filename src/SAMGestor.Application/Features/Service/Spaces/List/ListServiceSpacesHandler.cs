using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.List;

public sealed class ListServiceSpacesHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo
) : IRequestHandler<ListServiceSpacesQuery, ListServiceSpacesResponse>
{
    public async Task<ListServiceSpacesResponse> Handle(ListServiceSpacesQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        
        if (q.IsActive.HasValue)
            spaces = spaces.Where(s => s.IsActive == q.IsActive.Value).ToList();

        if (q.IsLocked.HasValue)
            spaces = spaces.Where(s => s.IsLocked == q.IsLocked.Value).ToList();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            spaces = spaces.Where(x =>
                x.Name.ToLowerInvariant().Contains(s) ||
                (x.Description ?? string.Empty).ToLowerInvariant().Contains(s)
            ).ToList();
        }

        var ids = spaces.Select(s => s.Id).ToArray();
        var assigns = await assignRepo.ListBySpaceIdsAsync(ids, ct);
        var allocatedBySpace = assigns.GroupBy(a => a.ServiceSpaceId)
                                      .ToDictionary(g => g.Key, g => g.Count());

        var items = spaces
            .OrderBy(s => s.Name)
            .Select(s => new ListItem(
                s.Id, s.Name, s.Description, s.IsActive, s.IsLocked,
                s.MinPeople, s.MaxPeople,
                allocatedBySpace.TryGetValue(s.Id, out var c) ? c : 0
            ))
            .ToList();

        return new ListServiceSpacesResponse(retreat.ServiceSpacesVersion, items);
    }
}
