using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Roster.Get;

public sealed class GetServiceRosterHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo,
    IServiceRegistrationRepository regRepo
) : IRequestHandler<GetServiceRosterQuery, GetServiceRosterResponse>
{
    public async Task<GetServiceRosterResponse> Handle(GetServiceRosterQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        if (spaces.Count == 0)
            return new GetServiceRosterResponse(retreat.ServiceSpacesVersion, Array.Empty<RosterSpaceView>());

        var spaceIds = spaces.Select(s => s.Id).ToArray();
        var assignments = await assignRepo.ListBySpaceIdsAsync(spaceIds, ct);

        var regIds = assignments.Select(a => a.ServiceRegistrationId).Distinct().ToArray();
        var regMap = await regRepo.GetMapByIdsAsync(regIds, ct);

        var membersBySpace = assignments
            .GroupBy(a => a.ServiceSpaceId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(x => x.Role == Domain.Enums.ServiceRole.Member ? 1 : 0) 
                .ThenBy(x => x.AssignedAt)
                .ToList());

        var list = new List<RosterSpaceView>(spaces.Count);
        foreach (var s in spaces)
        {
            membersBySpace.TryGetValue(s.Id, out var links);
            links ??= new List<Domain.Entities.ServiceAssignment>();

            var items = new List<RosterMemberView>(links.Count);
            int pos = 0;
            foreach (var l in links)
            {
                if (!regMap.TryGetValue(l.ServiceRegistrationId, out var reg)) continue;
                items.Add(new RosterMemberView(
                    reg.Id,
                    (string)reg.Name,
                    l.Role,
                    pos++,
                    reg.City
                ));
            }

            list.Add(new RosterSpaceView(
                s.Id, s.Name, s.Description,
                s.MinPeople, s.MaxPeople,
                s.IsLocked, s.IsActive,
                items
            ));
        }

        return new GetServiceRosterResponse(retreat.ServiceSpacesVersion, list);
    }
}
