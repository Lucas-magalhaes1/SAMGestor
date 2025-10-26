using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.Assign;

public sealed class UpdateTentRosterHandler(
    IRetreatRepository        retreatRepo,
    ITentRepository           tentRepo,
    ITentAssignmentRepository assignRepo,
    IRegistrationRepository   registrationRepo,
    IUnitOfWork               uow
) : IRequestHandler<UpdateTentRosterCommand, UpdateTentRosterResponse>
{
    public async Task<UpdateTentRosterResponse> Handle(UpdateTentRosterCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.TentsLocked)
            throw new BusinessRuleException("Barracas estão bloqueadas para edição.");

        if (retreat.TentsVersion != cmd.Version)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents:   Array.Empty<TentRosterSpaceView>(),
                Errors:  new[] { new TentRosterError("VERSION_MISMATCH", "Versão desatualizada. Recarregue as barracas.", null, Array.Empty<Guid>()) }
            );
        }

        // estado atual das barracas do retiro
        var tents   = await tentRepo.ListByRetreatAsync(cmd.RetreatId, category: null, active: null, ct: ct);
        var tentMap = tents.ToDictionary(t => t.Id, t => t);

        // valida barracas desconhecidas no snapshot
        var unknownTentIds = cmd.Tents.Where(t => !tentMap.ContainsKey(t.TentId))
                                      .Select(t => t.TentId)
                                      .Distinct()
                                      .ToList();
        if (unknownTentIds.Count > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents:   Array.Empty<TentRosterSpaceView>(),
                Errors:  new[] { new TentRosterError("UNKNOWN_TENT", "Algumas barracas não pertencem a este retiro.", null, unknownTentIds) }
            );
        }

        // carrega registros referenciados no snapshot
        var allRegIds = cmd.Tents.SelectMany(t => t.Members.Select(m => m.RegistrationId))
                                 .Distinct()
                                 .ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        var errors = new List<TentRosterError>();

        // registros inexistentes
        var missingRegs = allRegIds.Where(id => !regsMap.ContainsKey(id)).ToArray();
        if (missingRegs.Length > 0)
            errors.Add(new TentRosterError("UNKNOWN_REGISTRATION", "Alguns RegistrationIds não existem.", null, missingRegs));

        // registros de outro retiro
        var wrongRetreat = regsMap.Values.Where(r => r.RetreatId != cmd.RetreatId)
                                         .Select(r => r.Id)
                                         .ToArray();
        if (wrongRetreat.Length > 0)
            errors.Add(new TentRosterError("WRONG_RETREAT", "Alguns participantes pertencem a outro retiro.", null, wrongRetreat));

        if (errors.Count > 0)
            return new UpdateTentRosterResponse(retreat.TentsVersion, Array.Empty<TentRosterSpaceView>(), errors);

        // validações por barraca e duplicidade no snapshot
        var seenRegs = new HashSet<Guid>();

        foreach (var snap in cmd.Tents)
        {
            var tent = tentMap[snap.TentId];

            if (tent.IsLocked)
            {
                errors.Add(new TentRosterError("TENT_LOCKED", "Barraca bloqueada para edição.", tent.Id,
                    snap.Members.Select(m => m.RegistrationId).ToArray()));
                continue;
            }

            if (snap.Members.Count > tent.Capacity)
            {
                errors.Add(new TentRosterError("OVER_CAPACITY", $"Barraca excede a capacidade ({tent.Capacity}).", tent.Id,
                    snap.Members.Select(m => m.RegistrationId).ToArray()));
            }

            foreach (var m in snap.Members.OrderBy(x => x.Position))
            {
                if (!regsMap.TryGetValue(m.RegistrationId, out var reg))
                    continue; // já reportado

                // duplicidade no snapshot (mesma pessoa em +1 barraca)
                if (!seenRegs.Add(reg.Id))
                    errors.Add(new TentRosterError("DUPLICATED_MEMBER", "Participante aparece em mais de uma barraca.", tent.Id, new[] { reg.Id }));

                // apenas pagos/confirmados e habilitados — (regra do módulo “fazer”)
                var eligible = reg.Enabled &&
                               (reg.Status == RegistrationStatus.PaymentConfirmed || reg.Status == RegistrationStatus.Confirmed);

                if (!eligible)
                    errors.Add(new TentRosterError("INVALID_MEMBER", "Participante não está elegível para alocação (status).", tent.Id, new[] { reg.Id }));

                // checa gênero x categoria
                if ((tent.Category == TentCategory.Male   && reg.Gender != Gender.Male) ||
                    (tent.Category == TentCategory.Female && reg.Gender != Gender.Female))
                {
                    errors.Add(new TentRosterError("WRONG_CATEGORY", "Gênero do participante não coincide com a categoria da barraca.", tent.Id, new[] { reg.Id }));
                }
            }
        }

        if (errors.Count > 0)
            return new UpdateTentRosterResponse(retreat.TentsVersion, Array.Empty<TentRosterSpaceView>(), errors);

        // aplica snapshot
        var touchedTentIds = cmd.Tents.Select(t => t.TentId).Distinct().ToArray();

        var existingTouched = await assignRepo.ListByTentIdsAsync(touchedTentIds, ct);
        if (existingTouched.Count > 0)
            await assignRepo.RemoveRangeAsync(existingTouched, ct);

        var toAdd = new List<TentAssignment>(cmd.Tents.Sum(t => t.Members.Count));
        foreach (var snap in cmd.Tents)
        {
            var ordered = snap.Members.OrderBy(m => m.Position).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var m = ordered[i];
                toAdd.Add(new TentAssignment(
                    snap.TentId,
                    m.RegistrationId,
                    i
                ));
            }
        }

        if (toAdd.Count > 0)
            await assignRepo.AddRangeAsync(toAdd, ct);

        retreat.BumpTentsVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        // resposta
        var allAssignments = await assignRepo.ListByTentIdsAsync(tents.Select(t => t.Id).ToArray(), ct);
        var byTent = allAssignments.GroupBy(a => a.TentId)
                                   .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Position).ToList());

        var resp = await BuildResponseAsync(retreat.TentsVersion, tents, byTent, registrationRepo, ct);
        return resp;
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
