using MediatR;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.Get;

public sealed class GetTentRosterHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    ITentAssignmentRepository assignRepo,
    IRegistrationRepository regRepo
) : IRequestHandler<GetTentRosterQuery, GetTentRosterResponse>
{
    public async Task<GetTentRosterResponse> Handle(GetTentRosterQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        // Tende a ser alguns poucos dezenas/centenas
        var tents = await tentRepo.ListByRetreatAsync(q.RetreatId, category: null, active: null, ct);
        if (tents.Count == 0)
            return new GetTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new List<TentRosterSpaceView>()
            );

        var tentIds = tents.Select(t => t.Id).ToArray();
        var links   = await assignRepo.ListByTentIdsAsync(tentIds, ct);

        var regIds  = links.Select(l => l.RegistrationId).Distinct().ToArray();
        var regMap  = await regRepo.GetMapByIdsAsync(regIds, ct);

        var linksByTent = links.GroupBy(l => l.TentId)
                               .ToDictionary(g => g.Key, g => g.ToList());

        var views = new List<TentRosterSpaceView>(tents.Count);

        foreach (var t in tents.OrderBy(t => t.Number.Value))
        {
            linksByTent.TryGetValue(t.Id, out var tLinks);
            tLinks ??= new List<TentAssignment>();

            // ordenar por posição (nulls no fim), depois por AssignedAt
            var ordered = tLinks
                .OrderBy(l => l.Position ?? int.MaxValue)
                .ThenBy(l => l.AssignedAt)
                .ToList();

            var members = ordered
                .Select((l, idx) =>
                {
                    if (!regMap.TryGetValue(l.RegistrationId, out var r)) return null;

                    var pos = l.Position ?? idx; // coalesce para int
                    return new TentRosterMemberView(
                        r.Id,
                        (string)r.Name,
                        r.Gender.ToString(),
                        r.City,
                        pos
                    );
                })
                .Where(x => x is not null)!
                .ToList()!;

            views.Add(new TentRosterSpaceView(
                t.Id,
                t.Number.Value.ToString(),        // VO é int → string p/ o front
                t.Category.ToString(),
                t.Capacity,
                t.IsLocked,
                members
            ));
        }

        return new GetTentRosterResponse(
            Version: retreat.TentsVersion,
            Tents: views
        );
    }
}
