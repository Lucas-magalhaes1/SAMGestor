using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Locking;

public sealed class LockAllServiceSpacesHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IUnitOfWork uow
) : IRequestHandler<LockAllServiceSpacesCommand, LockAllServiceSpacesResponse>
{
    public async Task<LockAllServiceSpacesResponse> Handle(LockAllServiceSpacesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(cmd.RetreatId, ct);

        var changed = 0;
        foreach (var s in spaces)
        {
            if (cmd.Lock && !s.IsLocked) { s.Lock(); await spaceRepo.UpdateAsync(s, ct); changed++; }
            if (!cmd.Lock && s.IsLocked) { s.Unlock(); await spaceRepo.UpdateAsync(s, ct); changed++; }
        }

        if (changed > 0)
        {
            retreat.BumpServiceSpacesVersion();
            await retreatRepo.UpdateAsync(retreat, ct);
            await uow.SaveChangesAsync(ct);
        }

        return new LockAllServiceSpacesResponse(retreat.ServiceSpacesVersion, changed);
    }
}