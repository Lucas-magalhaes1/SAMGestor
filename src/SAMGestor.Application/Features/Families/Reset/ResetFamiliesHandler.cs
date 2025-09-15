using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
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

        // lock global sempre bloqueia
        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        var families = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        if (families.Count == 0)
        {
            // nada pra fazer
            return new ResetFamiliesResponse(
                Version: retreat.FamiliesVersion, FamiliesDeleted: 0, MembersDeleted: 0);
        }

        var lockedCount   = families.Count(f => f.IsLocked);
        var totalFamilies = families.Count;

        // regras de bloqueio por família
        if (lockedCount > 0 && !cmd.ForceLockedFamilies)
            throw new BusinessRuleException("Existem famílias bloqueadas. Use 'forceLockedFamilies=true' para prosseguir.");

        if (lockedCount == totalFamilies)
            throw new BusinessRuleException("Todas as famílias estão bloqueadas. Desbloqueie-as antes de resetar.");

        // contar membros antes de apagar
        var allMembers = await fmRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var membersCount = allMembers.Count;

        // apaga tudo (famílias + vínculos) — cascade já remove vínculos, mas pode manter a chamada explícita se preferir
        await familyRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);

        // bump versão
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
