using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums; 
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Reset;

public sealed class ResetFamiliesHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository fmRepo,
    IUnitOfWork uow
) : IRequestHandler<ResetFamiliesCommand, ResetFamiliesResponse>
{
    public async Task<ResetFamiliesResponse> Handle(ResetFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

       
        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        var families = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        if (families.Count == 0)
        {
            return new ResetFamiliesResponse(
                Version: retreat.FamiliesVersion,
                FamiliesDeleted: 0,
                MembersDeleted: 0
            );
        }

        
        var hasGroups = families.Any(f =>
            f.GroupStatus == GroupStatus.Creating ||
            f.GroupStatus == GroupStatus.Active
        );

        if (hasGroups)
            throw new BusinessRuleException(
                "Já existem grupos criados ou em criação. Não é permitido resetar as famílias."
            );

        
        var lockedCount = families.Count(f => f.IsLocked);
        var totalFamilies = families.Count;

        if (lockedCount > 0 && !cmd.ForceLockedFamilies)
        {
            throw new BusinessRuleException(
                $"Existem {lockedCount} família(s) bloqueada(s). " +
                "Use 'forceLockedFamilies=true' para prosseguir e deletar todas."
            );
        }

        if (lockedCount == totalFamilies)
        {
            throw new BusinessRuleException(
                "Todas as famílias estão bloqueadas. Desbloqueie-as antes de resetar."
            );
        }

       
        var allMembers = await fmRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var membersCount = allMembers.Count;

        
        await familyRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);

 
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new ResetFamiliesResponse(
            Version: retreat.FamiliesVersion,
            FamiliesDeleted: totalFamilies,
            MembersDeleted: membersCount
        );
    }
}
