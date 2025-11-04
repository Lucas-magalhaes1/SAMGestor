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

            var ordered = tLinks
                .OrderBy(l => l.Position ?? int.MaxValue)
                .ThenBy(l => l.AssignedAt)
                .ToList();

            var members = ordered
                .Select((l, idx) =>
                    regMap.TryGetValue(l.RegistrationId, out var r)
                        ? new TentRosterMemberView(
                            r.Id,
                            (string)r.Name,
                            r.Gender.ToString(),
                            r.City,
                            l.Position ?? idx)
                        : null
                )
                .OfType<TentRosterMemberView>()   
                .ToList();                        


            IReadOnlyList<TentRosterMemberView> safeMembers =
                members.Count == 0 ? Array.Empty<TentRosterMemberView>() : members;

            views.Add(new TentRosterSpaceView(
                t.Id,
                t.Number.Value.ToString(),
                t.Category.ToString(),
                t.Capacity,
                t.IsLocked,
                safeMembers
            ));
        }

        return new GetTentRosterResponse(
            Version: retreat.TentsVersion,
            Tents: views
        );
    }
}
