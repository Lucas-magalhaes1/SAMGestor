using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Lock;

public sealed class LockSingleFamilyHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IUnitOfWork uow
) : IRequestHandler<LockSingleFamilyCommand, LockSingleFamilyResponse>
{
    public async Task<LockSingleFamilyResponse> Handle(LockSingleFamilyCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");

        if (cmd.Lock) family.Lock();
        else          family.Unlock();

        await familyRepo.UpdateAsync(family, ct);
        
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new LockSingleFamilyResponse(family.Id, family.IsLocked, retreat.FamiliesVersion);
    }
}