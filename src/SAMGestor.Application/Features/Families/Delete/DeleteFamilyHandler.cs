using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Delete;

public sealed class DeleteFamilyHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IUnitOfWork uow
) : IRequestHandler<DeleteFamilyCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFamilyCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");

        if (family.IsLocked)
            throw new BusinessRuleException("Família está bloqueada e não pode ser removida.");

        await familyRepo.DeleteAsync(family, ct);

        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}