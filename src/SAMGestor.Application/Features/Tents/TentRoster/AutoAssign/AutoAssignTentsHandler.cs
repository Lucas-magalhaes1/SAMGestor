using MediatR;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.AutoAssign;

public sealed class AutoAssignTentsHandler(
    IRetreatRepository        retreatRepo,
    ITentRepository           tentRepo,
    ITentAssignmentRepository assignRepo,
    IRegistrationRepository   registrationRepo,
    IUnitOfWork               uow
) : IRequestHandler<AutoAssignTentsCommand, AutoAssignTentsResponse>
{
    public async Task<AutoAssignTentsResponse> Handle(AutoAssignTentsCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.TentsLocked)
            throw new BusinessRuleException("Barracas estão bloqueadas para edição.");

        var tentsAll = await tentRepo.ListByRetreatAsync(cmd.RetreatId, category: null, active: null, ct: ct);
        var tents    = cmd.RespectLocked ? tentsAll.Where(t => !t.IsLocked).ToList() : tentsAll;

        if (tents.Count == 0)
            return new AutoAssignTentsResponse(retreat.TentsVersion, Array.Empty<TentRosterSpaceView>());

        // ocupação atual
        var current = await assignRepo.ListByTentIdsAsync(tents.Select(t => t.Id).ToArray(), ct);
        var byTent  = current.GroupBy(a => a.TentId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Position).ToList());
        var assignedRegIds = current.Select(a => a.RegistrationId).Distinct().ToHashSet();

        // pools de elegíveis e não alocados (usa seu método pronto!)
        var poolMale   = await registrationRepo.ListPaidUnassignedAsync(cmd.RetreatId, Gender.Male,   null, ct);
        var poolFemale = await registrationRepo.ListPaidUnassignedAsync(cmd.RetreatId, Gender.Female, null, ct);

        // filtra só quem ainda não está alocado (por segurança)
        poolMale   = poolMale.Where(r => !assignedRegIds.Contains(r.Id)).ToList();
        poolFemale = poolFemale.Where(r => !assignedRegIds.Contains(r.Id)).ToList();

        // vagas restantes por barraca
        var remaining = tents.ToDictionary(
            t => t.Id,
            t => {
                var used = byTent.TryGetValue(t.Id, out var list) ? list.Count : 0;
                return Math.Max(0, t.Capacity - used);
            });

        var toAdd = new List<TentAssignment>();

        AssignBatch(poolMale,   tents.Where(t => t.Category == TentCategory.Male).ToList(),   remaining, byTent, toAdd, cmd.RetreatId);
        AssignBatch(poolFemale, tents.Where(t => t.Category == TentCategory.Female).ToList(), remaining, byTent, toAdd, cmd.RetreatId);

        if (toAdd.Count == 0)
        {
            // nada novo — devolve estado atual
            var resp0 = await BuildResponseAsync(retreat.TentsVersion, tentsAll, byTent, registrationRepo, ct);
            return new AutoAssignTentsResponse(retreat.TentsVersion, resp0.Tents);
        }

        await assignRepo.AddRangeAsync(toAdd, ct);

        retreat.BumpTentsVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        var finalAssignments = await assignRepo.ListByTentIdsAsync(tentsAll.Select(t => t.Id).ToArray(), ct);
        var byTentFinal = finalAssignments.GroupBy(a => a.TentId)
                                          .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Position).ToList());

        var resp = await BuildResponseAsync(retreat.TentsVersion, tentsAll, byTentFinal, registrationRepo, ct);
        return new AutoAssignTentsResponse(retreat.TentsVersion, resp.Tents);
    }

    private static void AssignBatch(
        List<Registration>                        pool,
        List<Tent>                                tents,
        Dictionary<Guid,int>                      remainingByTent,
        Dictionary<Guid,List<TentAssignment>>     byTent,
        List<TentAssignment>                      outLinks,
        Guid                                      retreatId)
    {
        if (pool.Count == 0 || tents.Count == 0) return;

        var ordered = tents
            .OrderBy(t => byTent.TryGetValue(t.Id, out var list) ? list.Count : 0) // menor ocupação primeiro
            .ThenBy(t => t.Number.Value)
            .ToList();

        foreach (var reg in pool)
        {
            var target = ordered.FirstOrDefault(t => remainingByTent.TryGetValue(t.Id, out var left) && left > 0);
            if (target is null) break;

            var pos = (byTent.TryGetValue(target.Id, out var list) ? list.Count : 0);

            var link = new TentAssignment(target.Id, reg.Id, pos);
            outLinks.Add(link);

            if (!byTent.TryGetValue(target.Id, out var lst))
            {
                lst = new List<TentAssignment>();
                byTent[target.Id] = lst;
            }
            lst.Add(link);

            remainingByTent[target.Id] = Math.Max(0, remainingByTent[target.Id] - 1);

            ordered = ordered
                .OrderBy(t => byTent.TryGetValue(t.Id, out var l) ? l.Count : 0)
                .ThenBy(t => t.Number.Value)
                .ToList();
        }
    }

    private static async Task<UpdateTentRosterResponse> BuildResponseAsync(
        int version,
        List<Tent> tents,
        Dictionary<Guid, List<TentAssignment>> byTent,
        IRegistrationRepository registrationRepo,
        CancellationToken ct)
    {
        var allRegIds = byTent.Values.SelectMany(v => v).Select(a => a.RegistrationId).Distinct().ToArray();
        var regsMap   = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        var views = new List<TentRosterSpaceView>(tents.Count);
        foreach (var tent in tents.OrderBy(t => t.Number.Value))
        {
            byTent.TryGetValue(tent.Id, out var links);
            links ??= new List<TentAssignment>();

            var members = links
                .OrderBy(l => l.Position ?? int.MaxValue)
                .Select((l, idx) =>
                {
                    var reg = regsMap[l.RegistrationId];
                    var pos = l.Position ?? idx;
                    return new TentRosterMemberView(
                        reg.Id,
                        (string)reg.Name,
                        reg.Gender.ToString(),
                        reg.City,
                        pos
                    );
                })
                .ToList();

            views.Add(new TentRosterSpaceView(
                tent.Id,
                tent.Number.Value.ToString(),
                tent.Category.ToString(),
                tent.Capacity,
                tent.IsLocked,
                members
            ));
        }

        return new UpdateTentRosterResponse(version, views, Array.Empty<TentRosterError>());
    }
}
