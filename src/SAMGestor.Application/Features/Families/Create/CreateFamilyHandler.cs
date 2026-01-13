using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Families.Create;

public sealed class CreateFamilyHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository fmRepo,
    IRegistrationRepository regRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateFamilyCommand, CreateFamilyResult>
{
    private const int MinimumMembers = 4;
    private const int MinimumCapacity = 4; 

    public async Task<CreateFamilyResult> Handle(CreateFamilyCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");
        
        if (cmd.MemberIds is null || cmd.MemberIds.Count < MinimumMembers)
            throw new BusinessRuleException($"É necessário informar no mínimo {MinimumMembers} membros.");

        if (cmd.MemberIds.Distinct().Count() != cmd.MemberIds.Count)
            throw new BusinessRuleException("Lista de membros contém IDs duplicados.");
        
        if (cmd.Capacity < MinimumCapacity)
            throw new BusinessRuleException($"Capacidade mínima é {MinimumCapacity} membros.");
        
        if (cmd.Capacity > 20)
            throw new BusinessRuleException("Capacidade máxima é 20 membros.");
        
        FamilyColor color;
        try
        {
            color = FamilyColor.FromName(cmd.ColorName);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        var colorExists = await familyRepo.ColorExistsInRetreatAsync(cmd.RetreatId, color.Name, null, ct);
        if (colorExists)
            throw new BusinessRuleException($"A cor '{color.Name}' já está sendo utilizada por outra família neste retiro.");
        
        var regsMap = await regRepo.GetMapByIdsAsync(cmd.MemberIds, ct);
        var notFound = cmd.MemberIds.Where(id => !regsMap.ContainsKey(id)).ToList();
        if (notFound.Count > 0)
            throw new NotFoundException("Registration", notFound.First());

        foreach (var r in regsMap.Values)
        {
            if (r.RetreatId != cmd.RetreatId)
                throw new BusinessRuleException("Todos os membros devem pertencer ao mesmo retiro.");

            if (!r.Enabled)
                throw new BusinessRuleException($"Participante '{r.Name}' está desabilitado.");

            if (r.Status is not RegistrationStatus.Confirmed and not RegistrationStatus.PaymentConfirmed)
                throw new BusinessRuleException($"Participante '{r.Name}' não está confirmado/pago.");
        }
        
        var existingLinks = await fmRepo.ListByRetreatAsync(cmd.RetreatId, ct) 
                            ?? new List<FamilyMember>();
        var alreadyAssigned = existingLinks.Select(x => x.RegistrationId).ToHashSet();
        var conflict = cmd.MemberIds.Where(id => alreadyAssigned.Contains(id)).ToList();
        if (conflict.Count > 0)
            throw new BusinessRuleException("Um ou mais membros já estão alocados em outra família.");
        
        if (cmd.PadrinhoIds != null && cmd.PadrinhoIds.Count > 0)
        {
            if (cmd.PadrinhoIds.Count > 2)
                throw new BusinessRuleException("Não é possível ter mais de 2 padrinhos.");

            foreach (var id in cmd.PadrinhoIds)
            {
                if (!cmd.MemberIds.Contains(id))
                    throw new BusinessRuleException("Todos os padrinhos devem estar na lista de membros.");

                if (regsMap[id].Gender != Gender.Male)
                    throw new BusinessRuleException($"Padrinho '{(string)regsMap[id].Name}' deve ser do gênero masculino.");
            }

            if (cmd.PadrinhoIds.Distinct().Count() != cmd.PadrinhoIds.Count)
                throw new BusinessRuleException("Os padrinhos devem ser pessoas diferentes.");
        }

        if (cmd.MadrinhaIds != null && cmd.MadrinhaIds.Count > 0)
        {
            if (cmd.MadrinhaIds.Count > 2)
                throw new BusinessRuleException("Não é possível ter mais de 2 madrinhas.");

            foreach (var id in cmd.MadrinhaIds)
            {
                if (!cmd.MemberIds.Contains(id))
                    throw new BusinessRuleException("Todas as madrinhas devem estar na lista de membros.");

                if (regsMap[id].Gender != Gender.Female)
                    throw new BusinessRuleException($"Madrinha '{(string)regsMap[id].Name}' deve ser do gênero feminino.");
            }

            if (cmd.MadrinhaIds.Distinct().Count() != cmd.MadrinhaIds.Count)
                throw new BusinessRuleException("As madrinhas devem ser pessoas diferentes.");
        }
        
        var warnings = await GenerateAlertsAsync(cmd.RetreatId, regsMap, cmd.Capacity, cmd.PadrinhoIds, cmd.MadrinhaIds, ct);
       
        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            return new CreateFamilyResult(
                Created: false,
                FamilyId: null,
                Version: retreat.FamiliesVersion,
                Warnings: warnings
            );
        }
        
        var familyName = await ResolveFamilyNameAsync(cmd.RetreatId, cmd.Name, ct);
        
        var family = new Family(new FamilyName(familyName), cmd.RetreatId, cmd.Capacity, color);
        await familyRepo.AddAsync(family, ct);

        var padrinhoSet = cmd.PadrinhoIds?.ToHashSet() ?? new HashSet<Guid>();
        var madrinhaSet = cmd.MadrinhaIds?.ToHashSet() ?? new HashSet<Guid>();

        var ordered = regsMap.Values
            .OrderBy(r => r.Gender)
            .ThenBy(r => r.Name.Value)
            .ToList();

        var links = ordered.Select((r, idx) =>
            new FamilyMember(
                cmd.RetreatId,
                family.Id,
                r.Id,
                position: idx,
                isPadrinho: padrinhoSet.Contains(r.Id),
                isMadrinha: madrinhaSet.Contains(r.Id)
            )
        ).ToList();

        await fmRepo.AddRangeAsync(links, ct);
        
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateFamilyResult(
            Created: true,
            FamilyId: family.Id,
            Version: retreat.FamiliesVersion,
            Warnings: warnings
        );
    }

    private async Task<List<CreateFamilyWarningDto>> GenerateAlertsAsync(
        Guid retreatId,
        Dictionary<Guid, Registration> regsMap,
        int capacity,
        IReadOnlyList<Guid>? padrinhoIds,
        IReadOnlyList<Guid>? madrinhaIds,
        CancellationToken ct)
    {
        var members = regsMap.Values.ToList();
        
        var alerts = FamilyAlertsCalculator.CalculateWithGodparents(
            members,
            padrinhoIds ?? Array.Empty<Guid>(),
            madrinhaIds ?? Array.Empty<Guid>(),
            expectedCapacity: capacity
        );
        
        var allFamilies = await familyRepo.ListByRetreatAsync(retreatId, ct);
        if (allFamilies.Count > 0)
        {
            var allMembersInRetreat = await fmRepo.ListByRetreatAsync(retreatId, ct);
            var sizesById = allMembersInRetreat.GroupBy(m => m.FamilyId).ToDictionary(g => g.Key, g => g.Count());
            var otherSizes = allFamilies.Select(f => sizesById.GetValueOrDefault(f.Id, 0)).Where(s => s > 0).ToList();

            if (otherSizes.Count > 0)
            {
                var sizeAlerts = FamilyAlertsCalculator.CheckFamilySizeComparison(
                    members.Count,
                    otherSizes,
                    members.Select(r => r.Id).ToList()
                );
                alerts.AddRange(sizeAlerts);
            }
        }

        return alerts.Select(a => new CreateFamilyWarningDto(
            a.Severity,
            a.Code,
            a.Message,
            a.RegistrationIds
        )).ToList();
    }

    private async Task<string> ResolveFamilyNameAsync(Guid retreatId, string? requestedName, CancellationToken ct)
    {
        var name = (requestedName ?? string.Empty).Trim();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            return await GenerateNextFamilyNameAsync(retreatId, ct);
        }

        var families = await familyRepo.ListByRetreatAsync(retreatId, ct);
        var existingNames = families.Select(f => ((string)f.Name).Trim().ToLowerInvariant()).ToHashSet();

        if (existingNames.Contains(name.ToLowerInvariant()))
        {
            throw new BusinessRuleException($"Já existe uma família com o nome '{name}' neste retiro.");
        }

        return name;
    }

    private async Task<string> GenerateNextFamilyNameAsync(Guid retreatId, CancellationToken ct)
    {
        var families = await familyRepo.ListByRetreatAsync(retreatId, ct);
        var used = new HashSet<int>();

        foreach (var f in families)
        {
            var value = ((string)f.Name).Trim();
            if (value.StartsWith("Família ", StringComparison.OrdinalIgnoreCase))
            {
                var tail = value["Família ".Length..].Trim();
                if (int.TryParse(tail, out var n) && n > 0)
                    used.Add(n);
            }
        }

        var i = 1;
        while (used.Contains(i)) i++;
        return $"Família {i}";
    }
}
