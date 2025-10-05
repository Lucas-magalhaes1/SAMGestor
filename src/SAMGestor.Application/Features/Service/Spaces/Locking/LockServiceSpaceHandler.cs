using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Locking;

public sealed class LockServiceSpaceHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IUnitOfWork uow
) : IRequestHandler<LockServiceSpaceCommand, LockServiceSpaceResponse>
{
    public async Task<LockServiceSpaceResponse> Handle(LockServiceSpaceCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var space = await spaceRepo.GetByIdAsync(cmd.SpaceId, ct)
                    ?? throw new NotFoundException(nameof(ServiceSpace), cmd.SpaceId);

        if (space.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Space não pertence a este retiro.");

        var changed = false;
        if (cmd.Lock && !space.IsLocked) { space.Lock(); changed = true; }     
        if (!cmd.Lock && space.IsLocked) { space.Unlock(); changed = true; }

        if (changed)
        {
            await spaceRepo.UpdateAsync(space, ct);
            retreat.BumpServiceSpacesVersion();
            await retreatRepo.UpdateAsync(retreat, ct);
            await uow.SaveChangesAsync(ct);
        }

        return new LockServiceSpaceResponse(retreat.ServiceSpacesVersion, changed);
    }
}