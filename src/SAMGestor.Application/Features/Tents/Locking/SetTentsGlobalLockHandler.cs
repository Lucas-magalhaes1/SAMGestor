using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed class SetTentsGlobalLockHandler(
    IRetreatRepository retreatRepo,
    IUnitOfWork uow
) : IRequestHandler<SetTentsGlobalLockCommand, SetTentsGlobalLockResponse>
{
    public async Task<SetTentsGlobalLockResponse> Handle(SetTentsGlobalLockCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        // tenta usar métodos do domínio; fallback em propriedade + bump
        var lockMethod   = retreat.GetType().GetMethod("LockTents",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var unlockMethod = retreat.GetType().GetMethod("UnlockTents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var bumpMethod   = retreat.GetType().GetMethod("BumpTentsVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var lockedProp   = retreat.GetType().GetProperty("TentsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var versionProp  = retreat.GetType().GetProperty("TentsVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var current = lockedProp is not null ? (bool)lockedProp.GetValue(retreat)! : false;

        if (cmd.Lock && !current)
        {
            if (lockMethod is not null) lockMethod.Invoke(retreat, null);
            else { lockedProp?.SetValue(retreat, true); bumpMethod?.Invoke(retreat, null); }
        }
        else if (!cmd.Lock && current)
        {
            if (unlockMethod is not null) unlockMethod.Invoke(retreat, null);
            else { lockedProp?.SetValue(retreat, false); bumpMethod?.Invoke(retreat, null); }
        }
        
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        var ver = versionProp is not null ? (int)versionProp.GetValue(retreat)! : 0;
        var nowLocked = lockedProp is not null ? (bool)lockedProp.GetValue(retreat)! : cmd.Lock;

        return new SetTentsGlobalLockResponse(retreat.Id, nowLocked, ver);
    }
}
