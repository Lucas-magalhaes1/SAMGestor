using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Summary;

public sealed class GetServiceSpacesSummaryHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo,
    IServiceRegistrationRepository regRepo
) : IRequestHandler<GetServiceSpacesSummaryQuery, GetServiceSpacesSummaryResponse>
{
    public async Task<GetServiceSpacesSummaryResponse> Handle(GetServiceSpacesSummaryQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        var spaceIds = spaces.Select(s => s.Id).ToArray();

        var assignments = await assignRepo.ListBySpaceIdsAsync(spaceIds, ct);
        var prefCounts  = await regRepo.CountPreferencesBySpaceAsync(q.RetreatId, ct);

        // índices de apoio
        var groupedBySpace = assignments.GroupBy(a => a.ServiceSpaceId)
                                        .ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<SpaceSummaryItem>(spaces.Count);
        foreach (var s in spaces.OrderBy(x => x.Name))
        {
            groupedBySpace.TryGetValue(s.Id, out var list);
            list ??= new List<Domain.Entities.ServiceAssignment>();

            var allocated = list.Count;
            var hasCoord  = list.Any(a => a.Role == ServiceRole.Coordinator);
            var hasVice   = list.Any(a => a.Role == ServiceRole.Vice);

            var prefs = prefCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0;

            items.Add(new SpaceSummaryItem(
                SpaceId: s.Id,
                Name: s.Name,
                IsActive: s.IsActive,
                IsLocked: s.IsLocked,
                MinPeople: s.MinPeople,
                MaxPeople: s.MaxPeople,
                Allocated: allocated,
                PreferredCount: prefs,
                HasCoordinator: hasCoord,
                HasVice: hasVice
            ));
        }

        return new GetServiceSpacesSummaryResponse(retreat.ServiceSpacesVersion, items);
    }
}
