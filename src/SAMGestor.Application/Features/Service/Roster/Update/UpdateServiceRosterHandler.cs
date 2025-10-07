using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Roster.Update;

public sealed class UpdateServiceRosterHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceRegistrationRepository regRepo,
    IServiceAssignmentRepository assignRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateServiceRosterCommand, UpdateServiceRosterResponse>
{
    public async Task<UpdateServiceRosterResponse> Handle(UpdateServiceRosterCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        // version check
        if (cmd.Version != retreat.ServiceSpacesVersion)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: new[] { new RosterError("VERSION_MISMATCH", "Versão desatualizada. Recarregue o quadro.", null, Array.Empty<Guid>()) },
                Warnings: Array.Empty<RosterWarning>()
            );
        }

        var spaces = await spaceRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var spacesMap = spaces.ToDictionary(s => s.Id, s => s);
        var locked = spaces.Where(s => s.IsLocked).Select(s => s.Id).ToHashSet();

        // espaços inválidos
        var unknownSpaces = cmd.Spaces.Where(s => !spacesMap.ContainsKey(s.SpaceId)).ToList();
        if (unknownSpaces.Count > 0)
        {
            var errs = unknownSpaces.Select(s =>
                new RosterError("UNKNOWN_SPACE", "SpaceId não pertence a este retiro.", s.SpaceId, Array.Empty<Guid>())
            ).ToArray();

            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: errs,
                Warnings: Array.Empty<RosterWarning>());
        }

        // carregar regs do payload
        var allRegIds = cmd.Spaces.SelectMany(s => s.Members.Select(m => m.RegistrationId)).Distinct().ToArray();
        var regsMap = await regRepo.GetMapByIdsAsync(allRegIds, ct);

        var missing = allRegIds.Where(id => !regsMap.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: new[] { new RosterError("UNKNOWN_REGISTRATION", "Alguns RegistrationIds não existem.", null, missing) },
                Warnings: Array.Empty<RosterWarning>());
        }

        var wrong = regsMap.Values.Where(r => r.RetreatId != cmd.RetreatId).Select(r => r.Id).ToArray();
        if (wrong.Length > 0)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: new[] { new RosterError("WRONG_RETREAT", "Alguns registros pertencem a outro retiro.", null, wrong) },
                Warnings: Array.Empty<RosterWarning>());
        }

        // validar locks
        var lockErrors = new List<RosterError>();
        foreach (var s in cmd.Spaces)
        {
            if (locked.Contains(s.SpaceId) && s.Members.Count > 0)
            {
                lockErrors.Add(new RosterError(
                    "SPACE_LOCKED",
                    $"Espaço '{spacesMap[s.SpaceId].Name}' está bloqueado para edição.",
                    s.SpaceId,
                    s.Members.Select(m => m.RegistrationId).ToArray()
                ));
            }
        }
        if (lockErrors.Count > 0)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: lockErrors,
                Warnings: Array.Empty<RosterWarning>());
        }

        // validar duplicidade de um mesmo reg em mais de um espaço
        var dupeErrors = new List<RosterError>();
        var seen = new HashSet<Guid>();
        foreach (var m in cmd.Spaces.SelectMany(s => s.Members))
        {
            if (!seen.Add(m.RegistrationId))
            {
                dupeErrors.Add(new RosterError(
                    "DUPLICATE_REGISTRATION",
                    "Um participante apareceu em mais de um espaço no payload.",
                    null,
                    new[] { m.RegistrationId }));
            }
        }
        if (dupeErrors.Count > 0)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: dupeErrors,
                Warnings: Array.Empty<RosterWarning>());
        }

        // invariantes de liderança por espaço (no payload)
        var leaderErrors = new List<RosterError>();
        foreach (var s in cmd.Spaces)
        {
            var coords = s.Members.Where(m => m.Role == ServiceRole.Coordinator).Select(m => m.RegistrationId).ToList();
            var vices  = s.Members.Where(m => m.Role == ServiceRole.Vice).Select(m => m.RegistrationId).ToList();
            if (coords.Count > 1)
                leaderErrors.Add(new RosterError("DUPLICATE_LEADER", $"Mais de um Coordenador em '{spacesMap[s.SpaceId].Name}'.", s.SpaceId, coords));
            if (vices.Count > 1)
                leaderErrors.Add(new RosterError("DUPLICATE_LEADER", $"Mais de um Vice em '{spacesMap[s.SpaceId].Name}'.", s.SpaceId, vices));
        }
        if (leaderErrors.Count > 0)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: Array.Empty<SpaceResult>(),
                Errors: leaderErrors,
                Warnings: Array.Empty<RosterWarning>());
        }

        // persistência: estratégia simples → remove tudo por espaço e re-insere ordenado (posições)
        foreach (var s in cmd.Spaces)
        {
            await assignRepo.RemoveBySpaceIdAsync(s.SpaceId, ct);

            var ordered = s.Members.OrderBy(m => m.Position).ToList();
            var toAdd = new List<ServiceAssignment>(ordered.Count);
            foreach (var (m, idx) in ordered.Select((m, i) => (m, i)))
            {
                toAdd.Add(new ServiceAssignment(
                    serviceSpaceId: s.SpaceId,
                    serviceRegistrationId: m.RegistrationId,
                    role: m.Role,
                    assignedBy: null // pode preencher com usuário gestor depois
                ));
            }

            if (toAdd.Count > 0)
                await assignRepo.AddRangeAsync(toAdd, ct);
        }

        // bump versão + salvar
        retreat.BumpServiceSpacesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        // calcular resumo + warnings pós-update
        var resultSpaces = new List<SpaceResult>();
        var warnings = new List<RosterWarning>();
        foreach (var s in cmd.Spaces)
        {
            var spec = spacesMap[s.SpaceId];
            var count = s.Members.Count;
            var hasCoord = s.Members.Any(m => m.Role == ServiceRole.Coordinator);
            var hasVice  = s.Members.Any(m => m.Role == ServiceRole.Vice);

            if (count < spec.MinPeople)
                warnings.Add(new RosterWarning("BelowMin", $"Alocados ({count}) abaixo do mínimo ({spec.MinPeople}) em '{spec.Name}'.", s.SpaceId));
            if (count > spec.MaxPeople)
                warnings.Add(new RosterWarning("OverMax", $"Alocados ({count}) acima do máximo ({spec.MaxPeople}) em '{spec.Name}'.", s.SpaceId));
            if (!hasCoord)
                warnings.Add(new RosterWarning("MissingCoordinator", $"Espaço '{spec.Name}' sem Coordenador.", s.SpaceId));
            if (!hasVice)
                warnings.Add(new RosterWarning("MissingVice", $"Espaço '{spec.Name}' sem Vice.", s.SpaceId));

            resultSpaces.Add(new SpaceResult(s.SpaceId, spec.Name, spec.MinPeople, spec.MaxPeople, count, hasCoord, hasVice));
        }

        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            return new UpdateServiceRosterResponse(
                Version: retreat.ServiceSpacesVersion,
                Spaces: resultSpaces,
                Errors: Array.Empty<RosterError>(),
                Warnings: warnings);
        }

        return new UpdateServiceRosterResponse(
            Version: retreat.ServiceSpacesVersion,
            Spaces: resultSpaces,
            Errors: Array.Empty<RosterError>(),
            Warnings: warnings);
    }
}
