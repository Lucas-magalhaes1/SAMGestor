using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Families.Update;

public sealed class UpdateFamiliesHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateFamiliesCommand, UpdateFamiliesResponse>
{
    public async Task<UpdateFamiliesResponse> Handle(UpdateFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
        {
            return new UpdateFamiliesResponse(
                Version: 0,
                Families: Array.Empty<FamilyDto>(),
                Errors: new[] { new FamilyErrorDto("RETREAT_NOT_FOUND", "Retiro não encontrado.", null, Array.Empty<Guid>()) },
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        if (cmd.Version != retreat.FamiliesVersion)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: new[] { new FamilyErrorDto("VERSION_MISMATCH", "Versão desatualizada. Recarregue as famílias.", null, Array.Empty<Guid>()) },
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        var currentFamilies = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var familiesMap = currentFamilies.ToDictionary(f => f.Id, f => f);
        var lockedIds = currentFamilies.Where(f => f.IsLocked).Select(f => f.Id).ToHashSet();

        var unknownFamilies = cmd.Families.Where(f => !familiesMap.ContainsKey(f.FamilyId)).ToList();
        if (unknownFamilies.Count > 0)
        {
            var errs = unknownFamilies.Select(f =>
                new FamilyErrorDto("UNKNOWN_FAMILY", "FamilyId não pertence a este retiro.", f.FamilyId, Array.Empty<Guid>())
            ).ToArray();

            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errs, Array.Empty<FamilyAlertDto>());
        }

        var lockedErrors = cmd.Families
            .Where(f => lockedIds.Contains(f.FamilyId))
            .Select(f => new FamilyErrorDto(
                "FAMILY_LOCKED",
                $"Família '{familiesMap[f.FamilyId].Name}' está bloqueada para edição.",
                f.FamilyId,
                f.Members.Select(m => m.RegistrationId).ToArray()
            ))
            .ToList();

        if (lockedErrors.Count > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: lockedErrors,
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        var allRegIds = cmd.Families.SelectMany(f => f.Members.Select(m => m.RegistrationId)).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        var missingRegs = allRegIds.Where(id => !regsMap.ContainsKey(id)).ToArray();
        if (missingRegs.Length > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: new[] { new FamilyErrorDto("UNKNOWN_REGISTRATION", "Alguns RegistrationIds não existem.", null, missingRegs) },
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        var wrongRetreat = regsMap.Values.Where(r => r.RetreatId != cmd.RetreatId).Select(r => r.Id).ToArray();
        if (wrongRetreat.Length > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: new[] { new FamilyErrorDto("WRONG_RETREAT", "Alguns participantes pertencem a outro retiro.", null, wrongRetreat) },
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        var colorErrors = new List<FamilyErrorDto>();
        var colorUsage = new Dictionary<string, Guid>();

        var editedFamilyIds = cmd.Families.Select(f => f.FamilyId).ToHashSet();
        foreach (var family in currentFamilies.Where(f => !editedFamilyIds.Contains(f.Id)))
        {
            colorUsage[family.Color.Name.ToLowerInvariant()] = family.Id;
        }

        foreach (var f in cmd.Families)
        {
            var colorNormalized = f.ColorName.Trim().ToLowerInvariant();
            
            try
            {
                FamilyColor.FromName(f.ColorName);
            }
            catch (ArgumentException)
            {
                colorErrors.Add(new FamilyErrorDto(
                    "INVALID_COLOR",
                    $"Cor '{f.ColorName}' não está disponível na lista predefinida.",
                    f.FamilyId,
                    Array.Empty<Guid>()));
                continue;
            }

            if (colorUsage.TryGetValue(colorNormalized, out var otherFamilyId) && otherFamilyId != f.FamilyId)
            {
                colorErrors.Add(new FamilyErrorDto(
                    "DUPLICATE_COLOR",
                    $"Cor '{f.ColorName}' já está sendo usada por outra família.",
                    f.FamilyId,
                    Array.Empty<Guid>()));
            }
            else
            {
                colorUsage[colorNormalized] = f.FamilyId;
            }
        }

        if (colorErrors.Count > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: colorErrors,
                Warnings: Array.Empty<FamilyAlertDto>());
        }

        var nameErrors = new List<FamilyErrorDto>();
        var nameUsage = new Dictionary<string, Guid>();

        foreach (var family in currentFamilies.Where(f => !editedFamilyIds.Contains(f.Id)))
        {
            nameUsage[((string)family.Name).ToLowerInvariant()] = family.Id;
        }

        foreach (var f in cmd.Families)
        {
            var nameNormalized = f.Name.Trim().ToLowerInvariant();

            if (nameUsage.TryGetValue(nameNormalized, out var otherFamilyId) && otherFamilyId != f.FamilyId)
            {
                nameErrors.Add(new FamilyErrorDto(
                    "DUPLICATE_NAME",
                    $"Já existe outra família com o nome '{f.Name}'.",
                    f.FamilyId,
                    Array.Empty<Guid>()));
            }
            else
            {
                nameUsage[nameNormalized] = f.FamilyId;
            }
        }

        if (nameErrors.Count > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: nameErrors,
                Warnings: Array.Empty<FamilyAlertDto>());
        }
        
        var godparentErrors = new List<FamilyErrorDto>();

        foreach (var f in cmd.Families)
        {
            var memberIds = f.Members.Select(m => m.RegistrationId).ToHashSet();
            var padrinhos = f.PadrinhoIds.Distinct().ToList();
            var madrinhas = f.MadrinhaIds.Distinct().ToList();

            var invalidPadrinhos = padrinhos.Where(id => !memberIds.Contains(id)).ToList();
            if (invalidPadrinhos.Count > 0)
            {
                godparentErrors.Add(new FamilyErrorDto(
                    "INVALID_PADRINHO",
                    "Todos os padrinhos devem ser membros da família.",
                    f.FamilyId,
                    invalidPadrinhos));
            }
            
            var invalidMadrinhas = madrinhas.Where(id => !memberIds.Contains(id)).ToList();
            if (invalidMadrinhas.Count > 0)
            {
                godparentErrors.Add(new FamilyErrorDto(
                    "INVALID_MADRINHA",
                    "Todas as madrinhas devem ser membros da família.",
                    f.FamilyId,
                    invalidMadrinhas));
            }

            var overlap = padrinhos.Intersect(madrinhas).ToList();
            if (overlap.Count > 0)
            {
                godparentErrors.Add(new FamilyErrorDto(
                    "GODPARENT_OVERLAP",
                    "Um membro não pode ser padrinho E madrinha.",
                    f.FamilyId,
                    overlap));
            }

            foreach (var id in padrinhos.Where(memberIds.Contains))
            {
                if (regsMap[id].Gender != Gender.Male)
                {
                    godparentErrors.Add(new FamilyErrorDto(
                        "INVALID_PADRINHO_GENDER",
                        $"Padrinho '{(string)regsMap[id].Name}' deve ser do gênero masculino.",  
                        f.FamilyId,
                        new[] { id }));
                }
            }

            foreach (var id in madrinhas.Where(memberIds.Contains))
            {
                if (regsMap[id].Gender != Gender.Female)
                {
                    godparentErrors.Add(new FamilyErrorDto(
                        "INVALID_MADRINHA_GENDER",
                        $"Madrinha '{(string)regsMap[id].Name}' deve ser do gênero feminino.",  
                        f.FamilyId,
                        new[] { id }));
                }
            }

        }

        if (godparentErrors.Count > 0)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: godparentErrors,
                Warnings: Array.Empty<FamilyAlertDto>());
        }
        
        var warnings = new List<FamilyAlertDto>();
        var allMembersForComparison = await familyMemberRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var sizesByFamilyId = allMembersForComparison.GroupBy(m => m.FamilyId).ToDictionary(g => g.Key, g => g.Count());

        foreach (var f in cmd.Families)
        {
            var registrations = f.Members
                .Select(m => regsMap[m.RegistrationId])
                .ToList();

            var alerts = FamilyAlertsCalculator.CalculateWithGodparents(
                registrations,
                f.PadrinhoIds,
                f.MadrinhaIds,
                f.Capacity
            );

            var otherSizes = cmd.Families
                .Where(other => other.FamilyId != f.FamilyId)
                .Select(other => other.Members.Count)
                .Concat(currentFamilies
                    .Where(cf => !editedFamilyIds.Contains(cf.Id))
                    .Select(cf => sizesByFamilyId.GetValueOrDefault(cf.Id, 0)))
                .Where(s => s > 0)
                .ToList();

            if (otherSizes.Count > 0)
            {
                var sizeAlerts = FamilyAlertsCalculator.CheckFamilySizeComparison(
                    f.Members.Count,
                    otherSizes,
                    registrations.Select(r => r.Id).ToList()
                );
                alerts.AddRange(sizeAlerts);
            }

            warnings.AddRange(alerts.Select(a => new FamilyAlertDto(
                a.Severity,
                a.Code,
                a.Message,
                a.RegistrationIds
            )));
        }

        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            return new UpdateFamiliesResponse(
                Version: retreat.FamiliesVersion,
                Families: Array.Empty<FamilyDto>(),
                Errors: Array.Empty<FamilyErrorDto>(),
                Warnings: warnings);
        }
        
        foreach (var f in cmd.Families)
        {
            var family = familiesMap[f.FamilyId];
            
            family.Rename(new FamilyName(f.Name.Trim()));
            family.SetCapacity(f.Capacity);
            family.ChangeColor(FamilyColor.FromName(f.ColorName));

            await familyRepo.UpdateAsync(family, ct);
            
            await familyMemberRepo.RemoveByFamilyIdAsync(f.FamilyId, ct);
            
            var ordered = f.Members.OrderBy(m => m.Position).ToList();
            var newLinks = new List<FamilyMember>();

            var padrinhoSet = f.PadrinhoIds.ToHashSet();
            var madrinhaSet = f.MadrinhaIds.ToHashSet();

            for (int k = 0; k < ordered.Count; k++)
            {
                var m = ordered[k];
                newLinks.Add(new FamilyMember(
                    cmd.RetreatId,
                    f.FamilyId,
                    m.RegistrationId,
                    position: k,
                    isPadrinho: padrinhoSet.Contains(m.RegistrationId),
                    isMadrinha: madrinhaSet.Contains(m.RegistrationId)
                ));
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

        var sizesById = membersByFamily.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        var familyDtos = familiesFinal.Select(f =>
        {
            membersByFamily.TryGetValue(f.Id, out var links);
            links ??= new List<FamilyMember>();

            var memberDtos = links
                .OrderBy(l => l.Position)
                .Select(l =>
                {
                    var reg = regsFinalMap[l.RegistrationId];
                    return new MemberDto(
                        l.RegistrationId,
                        (string)reg.Name,
                        reg.Email?.Value ?? string.Empty,
                        reg.Phone ?? string.Empty,
                        reg.Gender.ToString(),
                        reg.City ?? string.Empty,
                        l.Position,
                        l.IsPadrinho,
                        l.IsMadrinha
                    );
                })
                .ToList();

            var maleCount = memberDtos.Count(m => m.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase));
            var femaleCount = memberDtos.Count - maleCount;
            var totalMembers = memberDtos.Count;
            var remaining = Math.Max(0, f.Capacity - totalMembers);

            var malePercentage = totalMembers > 0 ? (maleCount * 100.0m / totalMembers) : 0m;
            var femalePercentage = totalMembers > 0 ? (femaleCount * 100.0m / totalMembers) : 0m;
            
            var registrations = links
                .Select(l => regsFinalMap[l.RegistrationId])
                .ToList();

            var padrinhos = links.Where(l => l.IsPadrinho).Select(l => l.RegistrationId).ToList();
            var madrinhas = links.Where(l => l.IsMadrinha).Select(l => l.RegistrationId).ToList();

            var finalAlerts = FamilyAlertsCalculator.CalculateWithGodparents(
                registrations,
                padrinhos,
                madrinhas,
                f.Capacity
            );

            var otherFinalSizes = familiesFinal
                .Where(of => of.Id != f.Id)
                .Select(of => sizesById.GetValueOrDefault(of.Id, 0))
                .Where(s => s > 0)
                .ToList();

            if (otherFinalSizes.Count > 0)
            {
                var finalSizeAlerts = FamilyAlertsCalculator.CheckFamilySizeComparison(
                    totalMembers,
                    otherFinalSizes,
                    registrations.Select(r => r.Id).ToList()
                );
                finalAlerts.AddRange(finalSizeAlerts);
            }

            return new FamilyDto(
                f.Id,
                (string)f.Name,
                f.Color.Name,
                f.Color.HexCode,
                f.Capacity,
                totalMembers,
                maleCount,
                femaleCount,
                malePercentage,
                femalePercentage,
                remaining,
                f.IsLocked,
                memberDtos,
                finalAlerts.Select(a => new FamilyAlertDto(a.Severity, a.Code, a.Message, a.RegistrationIds)).ToList()
            );
        }).ToList();

        return new UpdateFamiliesResponse(
            Version: retreat.FamiliesVersion,
            Families: familyDtos,
            Errors: Array.Empty<FamilyErrorDto>(),
            Warnings: warnings);
    }
}
