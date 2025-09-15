using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Lock;

public sealed class LockFamiliesHandler(
    IRetreatRepository retreatRepo,
    IUnitOfWork uow
) : IRequestHandler<LockFamiliesCommand, LockFamiliesResponse>
{
    public async Task<LockFamiliesResponse> Handle(LockFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException("Retreat", cmd.RetreatId);

        if (cmd.Lock) retreat.LockFamilies();
        else          retreat.UnlockFamilies();

        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        return new LockFamiliesResponse(retreat.FamiliesVersion, retreat.FamiliesLocked);
    }
}