using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.Update;

public sealed class UpdateTentRosterHandler(
    IRetreatRepository          retreatRepo,
    ITentRepository             tentRepo,
    ITentAssignmentRepository   assignRepo,
    IRegistrationRepository     regRepo,
    IUnitOfWork                 uow
) : IRequestHandler<UpdateTentRosterCommand, UpdateTentRosterResponse>
{
    public async Task<UpdateTentRosterResponse> Handle(UpdateTentRosterCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        // 1) Versão + Lock global
        if (retreat.TentsLocked)
            throw new BusinessRuleException("Barracas estão bloqueadas para edição.");

        if (cmd.Version != retreat.TentsVersion)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: new()
                {
                    new RosterError(
                        "VERSION_MISMATCH",
                        "Versão desatualizada. Recarregue o quadro.",
                        null,
                        Array.Empty<Guid>())
                },
                Warnings: new()
            );
        }

        // 2) Carregar estado atual das barracas referenciadas no payload
        var tentIdsPayload = cmd.Tents.Select(t => t.TentId).Distinct().ToArray();
        var allTents       = await tentRepo.ListByRetreatAsync(cmd.RetreatId, category: null, active: null, ct);
        var tentMap        = allTents.ToDictionary(t => t.Id, t => t);

        // 2.1) Barracas desconhecidas
        var unknown = tentIdsPayload.Where(id => !tentMap.ContainsKey(id)).ToList();
        if (unknown.Count > 0)
        {
            var errs = unknown.Select(id =>
                new RosterError("UNKNOWN_TENT", "TentId não pertence a este retiro.", id, Array.Empty<Guid>())
            ).ToList();

            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: errs,
                Warnings: new()
            );
        }

        var allRegIds = cmd.Tents.SelectMany(t => t.Members.Select(m => m.RegistrationId)).Distinct().ToArray();
        var regsMap   = await regRepo.GetMapByIdsAsync(allRegIds, ct);

        var missing = allRegIds.Where(id => !regsMap.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: new()
                {
                    new RosterError("UNKNOWN_REGISTRATION", "Alguns RegistrationIds não existem.", null, missing)
                },
                Warnings: new()
            );
        }

        var wrongRetreat = regsMap.Values.Where(r => r.RetreatId != cmd.RetreatId).Select(r => r.Id).ToArray();
        if (wrongRetreat.Length > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: new()
                {
                    new RosterError("WRONG_RETREAT", "Alguns participantes pertencem a outro retiro.", null, wrongRetreat)
                },
                Warnings: new()
            );
        }

        // 3.1) Regras do módulo: só "Fazer" e já pagos/confirmados
        var catErrors = new List<RosterError>();
        var paidErrors = new List<RosterError>();
        foreach (var rid in allRegIds)
        {
            var r = regsMap[rid];

            // considera Confirmed ou PaymentConfirmed como "pagos/aprovados"
            if (r.Status is not RegistrationStatus.Confirmed and not RegistrationStatus.PaymentConfirmed)
            {
                paidErrors.Add(new RosterError(
                    "NOT_PAID",
                    "Apenas participantes pagos/confirmados podem ser alocados.",
                    null, new[] { r.Id }));
            }
        }
        if (catErrors.Count > 0 || paidErrors.Count > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: catErrors.Concat(paidErrors).ToList(),
                Warnings: new()
            );
        }

        // 4) Duplicidade no payload (mesmo registration em >1 barraca)
        var seen = new HashSet<Guid>();
        var dupeRegs = new List<Guid>();
        foreach (var mid in allRegIds)
        {
            if (!seen.Add(mid)) dupeRegs.Add(mid);
        }
        if (dupeRegs.Count > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: new()
                {
                    new RosterError("DUPLICATE_REGISTRATION", "Um participante apareceu em mais de uma barraca no payload.", null, dupeRegs.Distinct().ToArray())
                },
                Warnings: new()
            );
        }

        // 5) Locks por barraca + validações de gênero/capacidade por barraca
        var lockErrors     = new List<RosterError>();
        var genderErrors   = new List<RosterError>();
        var capacityErrors = new List<RosterError>();
        var warnings       = new List<RosterWarning>();

        // Estado atual dos vínculos (apenas das barracas do payload)
        var currentLinks = await assignRepo.ListByTentIdsAsync(tentIdsPayload, ct);
        var currentByTent = currentLinks.GroupBy(l => l.TentId)
                                        .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var s in cmd.Tents)
        {
            var tent = tentMap[s.TentId];

            // Se barraca está bloqueada, não pode alterar snapshot
            if (tent.IsLocked)
            {
                // compara sets de registrationIds
                var current = currentByTent.TryGetValue(tent.Id, out var links)
                    ? links.Select(l => l.RegistrationId).OrderBy(x => x).ToArray()
                    : Array.Empty<Guid>();

                var proposed = s.Members.Select(m => m.RegistrationId).OrderBy(x => x).ToArray();

                if (!current.SequenceEqual(proposed))
                {
                    lockErrors.Add(new RosterError(
                        "TENT_LOCKED",
                        $"Barraca '{tent.Number.Value}' está bloqueada para edição.",
                        tent.Id,
                        proposed));
                    // não precisa checar mais regras nessa barraca
                    continue;
                }
            }

            // Gênero coerente com a categoria da barraca
            foreach (var m in s.Members)
            {
                var r = regsMap[m.RegistrationId];
                if ((tent.Category == TentCategory.Male   && r.Gender != Gender.Male) ||
                    (tent.Category == TentCategory.Female && r.Gender != Gender.Female))
                {
                    genderErrors.Add(new RosterError(
                        "GENDER_MISMATCH",
                        $"Gênero de '{r.Name}' não corresponde à categoria da barraca {tent.Number.Value}.",
                        tent.Id,
                        new[] { r.Id }));
                }
            }

            // Capacidade
            var count = s.Members.Count;
            if (count > tent.Capacity)
            {
                capacityErrors.Add(new RosterError(
                    "CAPACITY_OVERFLOW",
                    $"Alocados ({count}) acima da capacidade ({tent.Capacity}) na barraca {tent.Number.Value}.",
                    tent.Id,
                    s.Members.Select(m => m.RegistrationId).ToArray()));
            }
            else if (count < tent.Capacity)
            {
                warnings.Add(new RosterWarning(
                    "BelowCapacity",
                    $"Alocados ({count}) abaixo da capacidade ({tent.Capacity}) na barraca {tent.Number.Value}.",
                    tent.Id));
            }
        }

        var allErrors = lockErrors.Concat(genderErrors).Concat(capacityErrors).ToList();
        if (allErrors.Count > 0)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: allErrors,
                Warnings: warnings
            );
        }

        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            return new UpdateTentRosterResponse(
                Version: retreat.TentsVersion,
                Tents: new(),
                Errors: new(),
                Warnings: warnings
            );
        }

        // 6) Persistência — remove tudo das barracas do payload e reinsere ordenado
        if (tentIdsPayload.Length > 0)
        {
            if (currentLinks.Count > 0)
                await assignRepo.RemoveRangeAsync(currentLinks, ct);

            var toAdd = new List<TentAssignment>(allRegIds.Length);
            foreach (var s in cmd.Tents)
            {
                var membersOrdered = s.Members.OrderBy(m => m.Position).ToList();
                foreach (var m in membersOrdered)
                {
                    toAdd.Add(new TentAssignment(
                        tentId: s.TentId,
                        registrationId: m.RegistrationId,
                        position: m.Position
                    ));
                }
            }

            if (toAdd.Count > 0)
                await assignRepo.AddRangeAsync(toAdd, ct);
        }

        retreat.BumpTentsVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        var results = new List<TentResult>(cmd.Tents.Count);
        foreach (var s in cmd.Tents)
        {
            var t = tentMap[s.TentId];
            var count = s.Members.Count;
            results.Add(new TentResult(
                TentId: t.Id,
                Number: t.Number.Value.ToString(),
                Capacity: t.Capacity,
                AssignedCount: count,
                Remaining: t.Capacity - count
            ));
        }

        return new UpdateTentRosterResponse(
            Version: retreat.TentsVersion,
            Tents: results,
            Errors: new(),
            Warnings: warnings // podem vir vazios (ou ter sido ignorados quando IgnoreWarnings=true)
        );
    }
}
