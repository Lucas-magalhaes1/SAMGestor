using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Application.Interfaces;

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
    // Para MVP: famílias fixas de 4 com 2M + 2F
    private const int FixedCapacity = 4;

    public async Task<UpdateFamiliesResponse> Handle(UpdateFamiliesCommand cmd, CancellationToken ct)
    {
        // 1) Checar retiro + versão
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
        {
            return new UpdateFamiliesResponse(0, Array.Empty<FamilyDto>(),
                new[] { new FamilyErrorDto("NOT_FOUND", "Retreat não encontrado.", null, Array.Empty<Guid>()) },
                Array.Empty<FamilyAlertDto>());
        }

        if (cmd.Version != retreat.FamiliesVersion)
        {
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(),
                new[] { new FamilyErrorDto("VERSION_MISMATCH", "Versão desatualizada. Recarregue as famílias.", null, Array.Empty<Guid>()) },
                Array.Empty<FamilyAlertDto>());
        }

        // 2) Carregar famílias atuais do retiro e validar FamilyIds
        var families = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var familiesMap = families.ToDictionary(f => f.Id, f => f);

        var unknownFamilies = cmd.Families.Where(f => !familiesMap.ContainsKey(f.FamilyId)).ToList();
        if (unknownFamilies.Count > 0)
        {
            var errs = unknownFamilies.Select(f =>
                new FamilyErrorDto("UNKNOWN_FAMILY", "FamilyId não pertence a este retiro.", f.FamilyId, Array.Empty<Guid>())
            ).ToList();

            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errs, Array.Empty<FamilyAlertDto>());
        }

        // 3) Coletar todos ids de registration do snapshot e buscar dados
        var allRegIds = cmd.Families.SelectMany(f => f.Members.Select(m => m.RegistrationId)).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        // Checar se todos existem e pertencem ao retiro
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

        // 4) Validação por família (capacidade/2M2F/parentesco/sobrenome) + warnings (cidade)
        var errors = new List<FamilyErrorDto>();
        var warnings = new List<FamilyAlertDto>();

        foreach (var f in cmd.Families)
        {
            // Capacidade: para MVP fixo (=4), validar exatamente igual
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

            // 2M+2F
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

            // Parentesco/cônjuge + Sobrenome (crítico)
            var regsArr = regs.ToArray();
            for (int i = 0; i < regsArr.Length; i++)
            for (int j = i + 1; j < regsArr.Length; j++)
            {
                var ri = regsArr[i];
                var rj = regsArr[j];

                // AreSpouses / AreDirectRelatives (forte) => erro
                if (await relationshipService.AreSpousesAsync(ri.Id, rj.Id, ct)
                 || await relationshipService.AreDirectRelativesAsync(ri.Id, rj.Id, ct))
                {
                    errors.Add(new FamilyErrorDto(
                        Code: "RELATIONSHIP_CONFLICT",
                        Message: "Parentes/cônjuges não podem ficar na mesma família.",
                        FamilyId: f.FamilyId,
                        RegistrationIds: new[] { ri.Id, rj.Id }));
                }

                // Sobrenome (heurística crítica no MVP)
                var lastI = ExtractLastName((string)ri.Name);
                var lastJ = ExtractLastName((string)rj.Name);
                if (!string.IsNullOrWhiteSpace(lastI) &&
                    NormalizeSurname(lastI) == NormalizeSurname(lastJ))
                {
                    errors.Add(new FamilyErrorDto(
                        Code: "SAME_SURNAME",
                        Message: $"Sobrenome repetido na mesma família: '{NormalizeSurname(lastI)}'.",
                        FamilyId: f.FamilyId,
                        RegistrationIds: new[] { ri.Id, rj.Id }));
                }
            }

            // Warnings: mesma cidade
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
        {
            // Não persiste; retorna erros
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), errors, warnings);
        }

        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            // Retorna warnings para o front decidir se manda novamente com IgnoreWarnings=true
            return new UpdateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<FamilyDto>(), Array.Empty<FamilyErrorDto>(), warnings);
        }

        // 5) Aplicar DELTA (atualizar nome/capacidade; substituir membros por família)
        foreach (var f in cmd.Families)
        {
            var family = familiesMap[f.FamilyId];

            // Atualiza nome/capacidade
            family.Rename(new Domain.ValueObjects.FullName(f.Name));
            family.SetCapacity(f.Capacity);
            await familyRepo.UpdateAsync(family, ct);

            // Substituição dos membros (mais simples e segura para snapshot)
            await familyMemberRepo.RemoveByFamilyIdAsync(f.FamilyId, ct);

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

        // 6) Incrementar versão e salvar
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        // 7) Montar resposta com estado final
        // Reaproveita o caminho do GetAll para projetar DTOs
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
