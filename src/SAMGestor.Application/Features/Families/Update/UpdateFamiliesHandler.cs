using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Exceptions;

namespace SAMGestor.Application.Features.Families.Update;

public sealed class UpdateFamiliesHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IRelationshipService relationshipService,
    IUnitOfWork uow
) : IRequestHandler<UpdateFamiliesCommand, UpdateFamiliesResponse>
{
    private const int FixedCapacity = 4;

    public async Task<UpdateFamiliesResponse> Handle(UpdateFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
        {
            return new UpdateFamiliesResponse(0, Array.Empty<FamilyDto>(),
                new[] { new FamilyErrorDto("NOT_FOUND", "Retreat não encontrado.", null, Array.Empty<Guid>()) },
                Array.Empty<FamilyAlertDto>());
        }

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        if (cmd.Version != retreat.FamiliesVersion)
        {
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(),
                new[] { new FamilyErrorDto("VERSION_MISMATCH", "Versão desatualizada. Recarregue as famílias.", null, Array.Empty<Guid>()) },
                Array.Empty<FamilyAlertDto>());
        }

        // estado atual
        var currentFamilies = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var currentMembers  = await familyMemberRepo.ListByFamilyIdsAsync(currentFamilies.Select(f => f.Id), ct);
        var lockedIds = currentFamilies.Where(f => f.IsLocked).Select(f => f.Id).ToHashSet();

        // snapshot recebido
        var snapByFamily = cmd.Families.ToDictionary(
            f => f.FamilyId,
            f => new {
                Name = f.Name?.Trim(),
                Regs = f.Members.Select(m => m.RegistrationId).ToHashSet(),
                Positions = f.Members.OrderBy(m => m.Position).Select(m => (m.RegistrationId, m.Position)).ToList()
            });

        // valida tentativas de alterar famílias travadas
        var lockedErrors = new List<FamilyErrorDto>();

        foreach (var lockedId in lockedIds)
        {
            var existing = currentFamilies.First(x => x.Id == lockedId);
            var existingLinks = currentMembers.GetValueOrDefault(lockedId) ?? new List<FamilyMember>();

            var existingRegs = existingLinks.Select(l => l.RegistrationId).ToHashSet();
            var existingPositions = existingLinks.OrderBy(l => l.Position).Select(l => (l.RegistrationId, l.Position)).ToList();

            if (!snapByFamily.TryGetValue(lockedId, out var snap))
            {
                lockedErrors.Add(new FamilyErrorDto(
                    Code: "FAMILY_LOCKED",
                    Message: "Família bloqueada não pode ser removida ou alterada.",
                    FamilyId: lockedId,
                    RegistrationIds: Array.Empty<Guid>()));
                continue;
            }

            var nameChanged = !string.Equals(snap.Name ?? (string)existing.Name, (string)existing.Name, StringComparison.Ordinal);
            var membersChanged = !existingRegs.SetEquals(snap.Regs);
            var positionsChanged = !existingPositions.SequenceEqual(snap.Positions);

            if (nameChanged || membersChanged || positionsChanged)
            {
                lockedErrors.Add(new FamilyErrorDto(
                    Code: "FAMILY_LOCKED",
                    Message: "Família bloqueada não pode ser alterada.",
                    FamilyId: lockedId,
                    RegistrationIds: snap.Regs.ToList()));
            }
        }

        if (lockedErrors.Count > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: lockedErrors,
                Warnings: Array.Empty<FamilyAlertDto>());
        }
        
        var families = currentFamilies;
        var familiesMap = families.ToDictionary(f => f.Id, f => f);

        var unknownFamilies = cmd.Families.Where(f => !familiesMap.ContainsKey(f.FamilyId)).ToList();
        if (unknownFamilies.Count > 0)
        {
            var errs = unknownFamilies.Select(f =>
                new FamilyErrorDto("UNKNOWN_FAMILY", "FamilyId não pertence a este retiro.", f.FamilyId, Array.Empty<Guid>())
            ).ToList();

            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errs, Array.Empty<FamilyAlertDto>());
        }

        var allRegIds = cmd.Families.SelectMany(f => f.Members.Select(m => m.RegistrationId)).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        var missingRegs = allRegIds.Where(id => !regsMap.ContainsKey(id)).ToArray();
        if (missingRegs.Length > 0)
        {
            var errs = new List<FamilyErrorDto> {
                new FamilyErrorDto("UNKNOWN_REGISTRATION", "Alguns RegistrationIds não existem.", null, missingRegs)
            };
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errs, Array.Empty<FamilyAlertDto>());
        }

        var wrongRetreat = regsMap.Values.Where(r => r.RetreatId != cmd.RetreatId).Select(r => r.Id).ToArray();
        if (wrongRetreat.Length > 0)
        {
            var errs = new List<FamilyErrorDto> {
                new FamilyErrorDto("WRONG_RETREAT", "Alguns participantes pertencem a outro retiro.", null, wrongRetreat)
            };
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errs, Array.Empty<FamilyAlertDto>());
        }

        var errors = new List<FamilyErrorDto>();
        var warnings = new List<FamilyAlertDto>();

        foreach (var f in cmd.Families)
        {
            if (f.Capacity != FixedCapacity)
            {
                errors.Add(new FamilyErrorDto(
                    Code: "CAPACITY_INVALID",
                    Message: $"Capacidade deve ser {FixedCapacity} no MVP.",
                    FamilyId: f.FamilyId,
                    RegistrationIds: Array.Empty<Guid>()));
            }

            if (f.Members.Count != f.Capacity)
            {
                errors.Add(new FamilyErrorDto(
                    Code: "CAPACITY_MISMATCH",
                    Message: $"Família deve possuir exatamente {f.Capacity} membros.",
                    FamilyId: f.FamilyId,
                    RegistrationIds: f.Members.Select(m => m.RegistrationId).ToArray()));
            }

            var regs = f.Members.Select(m => regsMap[m.RegistrationId]).ToList();
            var maleCount = regs.Count(r => r.Gender == Gender.Male);
            var femaleCount = regs.Count - maleCount;
            if (maleCount != 2 || femaleCount != 2)
            {
                errors.Add(new FamilyErrorDto(
                    Code: "COMPOSITION_INVALID",
                    Message: "Composição obrigatória: 2 homens e 2 mulheres.",
                    FamilyId: f.FamilyId,
                    RegistrationIds: regs.Select(r => r.Id).ToArray()));
            }

            var regsArr = regs.ToArray();
            for (int i = 0; i < regsArr.Length; i++)
            for (int j = i + 1; j < regsArr.Length; j++)
            {
                var ri = regsArr[i];
                var rj = regsArr[j];

                if (await relationshipService.AreSpousesAsync(ri.Id, rj.Id, ct)
                 || await relationshipService.AreDirectRelativesAsync(ri.Id, rj.Id, ct))
                {
                    errors.Add(new FamilyErrorDto(
                        Code: "RELATIONSHIP_CONFLICT",
                        Message: "Parentes/cônjuges não podem ficar na mesma família.",
                        FamilyId: f.FamilyId,
                        RegistrationIds: new[] { ri.Id, rj.Id }));
                }

                var lastI = ExtractLastName((string)ri.Name);
                var lastJ = ExtractLastName((string)rj.Name);
                if (!string.IsNullOrWhiteSpace(lastI)
                 && NormalizeSurname(lastI) == NormalizeSurname(lastJ))
                {
                    errors.Add(new FamilyErrorDto(
                        Code: "SAME_SURNAME",
                        Message: $"Sobrenome repetido na mesma família: '{NormalizeSurname(lastI)}'.",
                        FamilyId: f.FamilyId,
                        RegistrationIds: new[] { ri.Id, rj.Id }));
                }
            }

            var cityGroups = regs
                .GroupBy(r => (r.City ?? string.Empty).Trim().ToLowerInvariant())
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

            foreach (var g in cityGroups)
            {
                warnings.Add(new FamilyAlertDto(
                    Severity: "warning",
                    Code: "SAME_CITY",
                    Message: $"Múltiplos membros da mesma cidade: '{g.Key}'.",
                    RegistrationIds: g.Select(r => r.Id).ToList()));
            }
        }

        if (errors.Count > 0)
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errors, warnings);

        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), Array.Empty<FamilyErrorDto>(), warnings);

        foreach (var f in cmd.Families)
        {
            var family = currentFamilies.First(x => x.Id == f.FamilyId);

            family.Rename(new Domain.ValueObjects.FamilyName(f.Name));
            family.SetCapacity(f.Capacity);
            await familyRepo.UpdateAsync(family, ct);
            
            await familyMemberRepo.RemoveByFamilyIdAsync(f.FamilyId, ct);
            
            await uow.SaveChangesAsync(ct); 
            
            var ordered = f.Members.OrderBy(m => m.Position).ToList();
            var newLinks = new List<FamilyMember>(ordered.Count);
            for (int k = 0; k < ordered.Count; k++)
            {
                var m = ordered[k];
                newLinks.Add(new FamilyMember(cmd.RetreatId, f.FamilyId, m.RegistrationId, position: k));
            }

            if (newLinks.Count > 0)
                await familyMemberRepo.AddRangeAsync(newLinks, ct);
        }

        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        var familiesFinal = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var membersByFamily = await familyMemberRepo.ListByFamilyIdsAsync(familiesFinal.Select(x => x.Id), ct);
        var allFinalIds = membersByFamily.Values.SelectMany(v => v).Select(m => m.RegistrationId).Distinct().ToArray();
        var regsFinalMap = await registrationRepo.GetMapByIdsAsync(allFinalIds, ct);

        var familyDtos = new List<FamilyDto>(familiesFinal.Count);
        foreach (var f in familiesFinal)
        {
            membersByFamily.TryGetValue(f.Id, out var links);
            links ??= new List<FamilyMember>();

            var memberViews = links
                .OrderBy(l => l.Position)
                .Select(l =>
                {
                    var reg = regsFinalMap[l.RegistrationId];
                    return new FamilyRead.MemberView(reg.Id, (string)reg.Name, reg.Gender, reg.City, l.Position);
                })
                .ToList();

            var (total, male, female, remaining) = FamilyRead.Metrics(f.Capacity, memberViews);
            var alerts = FamilyRead.Alerts(memberViews)
                .Select(a => new FamilyAlertDto(a.Severity, a.Code, a.Message, a.RegistrationIds))
                .ToList();

            familyDtos.Add(new FamilyDto(
                f.Id,
                (string)f.Name,
                f.Capacity,
                total,
                male,
                female,
                remaining,
                memberViews.Select(v => new MemberDto(v.RegistrationId, v.Name, v.Gender.ToString(), v.City, v.Position)).ToList(),
                alerts
            ));
        }

        return new UpdateFamiliesResponse(retreat.FamiliesVersion, familyDtos, Array.Empty<FamilyErrorDto>(), Array.Empty<FamilyAlertDto>());
    }

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    private static string NormalizeSurname(string surname)
    {
        if (string.IsNullOrWhiteSpace(surname)) return string.Empty;
        var s = surname.Trim().ToLowerInvariant();
        if (s is "de" or "da" or "do" or "dos" or "das") return string.Empty;
        return s;
    }
}
