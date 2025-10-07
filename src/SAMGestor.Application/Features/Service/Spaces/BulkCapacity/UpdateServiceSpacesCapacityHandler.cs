using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.BulkCapacity;

public sealed class UpdateServiceSpacesCapacityHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateServiceSpacesCapacityCommand, UpdateServiceSpacesCapacityResponse>
{
    public async Task<UpdateServiceSpacesCapacityResponse> Handle(UpdateServiceSpacesCapacityCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var map    = spaces.ToDictionary(s => s.Id, s => s);

        var updated = 0;
        var skipped = new List<Guid>();
        var changed = false;

        if (cmd.ApplyToAll)
        {
            foreach (var s in spaces)
            {
                if (s.IsLocked) { skipped.Add(s.Id); continue; }

                var hasChange = (s.MinPeople != cmd.MinPeople!.Value) || (s.MaxPeople != cmd.MaxPeople!.Value);
                if (!hasChange) continue;

                s.UpdateCapacity(cmd.MinPeople!.Value, cmd.MaxPeople!.Value);
                await spaceRepo.UpdateAsync(s, ct);
                updated++;
                changed = true;
            }
        }
        else
        {
            foreach (var it in cmd.Items!)
            {
                if (!map.TryGetValue(it.SpaceId, out var s)) continue;
                if (s.IsLocked) { skipped.Add(s.Id); continue; }

                var hasChange = (s.MinPeople != it.MinPeople) || (s.MaxPeople != it.MaxPeople);
                if (!hasChange) continue;

                s.UpdateCapacity(it.MinPeople, it.MaxPeople);
                await spaceRepo.UpdateAsync(s, ct);
                updated++;
                changed = true;
            }
        }

        if (changed)
        {
            retreat.BumpServiceSpacesVersion();
            await retreatRepo.UpdateAsync(retreat, ct);
        }

        await uow.SaveChangesAsync(ct);

        return new UpdateServiceSpacesCapacityResponse(
            Version: retreat.ServiceSpacesVersion,
            UpdatedCount: updated,
            SkippedLocked: skipped
        );
    }
}
