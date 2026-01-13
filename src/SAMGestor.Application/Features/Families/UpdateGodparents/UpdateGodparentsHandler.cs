using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.UpdateGodparents;

public sealed class UpdateGodparentsHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateGodparentsCommand, UpdateGodparentsResult>
{
    public async Task<UpdateGodparentsResult> Handle(UpdateGodparentsCommand cmd, CancellationToken ct)
    {
        var warnings = new List<string>();

        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");

        if (family.IsLocked)
            throw new BusinessRuleException("Esta família está bloqueada para edição.");

        var padrinhoIds = (cmd.PadrinhoIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var madrinhaIds = (cmd.MadrinhaIds ?? Array.Empty<Guid>()).Distinct().ToList();

        if (padrinhoIds.Count > 2)
            throw new BusinessRuleException("Não é possível ter mais de 2 padrinhos.");

        if (madrinhaIds.Count > 2)
            throw new BusinessRuleException("Não é possível ter mais de 2 madrinhas.");

        var familyMembers = await familyMemberRepo.ListByFamilyAsync(family.Id, ct);
        var memberIds = familyMembers.Select(m => m.RegistrationId).ToHashSet();

        var invalidPadrinhos = padrinhoIds.Where(id => !memberIds.Contains(id)).ToList();
        if (invalidPadrinhos.Count > 0)
            throw new BusinessRuleException("Um ou mais padrinhos informados não pertencem a esta família.");

        var invalidMadrinhas = madrinhaIds.Where(id => !memberIds.Contains(id)).ToList();
        if (invalidMadrinhas.Count > 0)
            throw new BusinessRuleException("Uma ou mais madrinhas informadas não pertencem a esta família.");

        var overlap = padrinhoIds.Intersect(madrinhaIds).ToList();
        if (overlap.Count > 0)
            throw new BusinessRuleException("Um membro não pode ser padrinho E madrinha ao mesmo tempo.");

        var allIds = padrinhoIds.Concat(madrinhaIds).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(allIds, ct);

        foreach (var id in padrinhoIds)
        {
            if (regsMap.TryGetValue(id, out var reg) && reg.Gender != Gender.Male)
                throw new BusinessRuleException($"Padrinho '{reg.Name}' deve ser do gênero masculino.");
        }

        foreach (var id in madrinhaIds)
        {
            if (regsMap.TryGetValue(id, out var reg) && reg.Gender != Gender.Female)
                throw new BusinessRuleException($"Madrinha '{reg.Name}' deve ser do gênero feminino.");
        }

        if (padrinhoIds.Count < 2)
            warnings.Add($"Família tem apenas {padrinhoIds.Count} padrinho(s). Recomendado: 2.");

        if (madrinhaIds.Count < 2)
            warnings.Add($"Família tem apenas {madrinhaIds.Count} madrinha(s). Recomendado: 2.");

        if (padrinhoIds.Count == 0 && madrinhaIds.Count == 0)
            warnings.Add("Família não possui padrinhos nem madrinhas definidos.");

        foreach (var member in familyMembers)
        {
            member.UnmarkAsPadrinho();
            member.UnmarkAsMadrinha();
        }
        
        foreach (var member in familyMembers)
        {
            if (padrinhoIds.Contains(member.RegistrationId))
                member.MarkAsPadrinho();

            if (madrinhaIds.Contains(member.RegistrationId))
                member.MarkAsMadrinha();
        }
        
        await familyMemberRepo.UpdateRangeAsync(familyMembers, ct);

        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        return new UpdateGodparentsResult(
            Success: true,
            Version: retreat.FamiliesVersion,
            Warnings: warnings
        );
    }
}
