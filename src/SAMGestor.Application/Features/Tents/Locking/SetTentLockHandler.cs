using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed class SetTentLockHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    IUnitOfWork uow
) : IRequestHandler<SetTentLockCommand, SetTentLockResponse>
{
    public async Task<SetTentLockResponse> Handle(SetTentLockCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var tent = await tentRepo.GetByIdAsync(cmd.TentId, ct)
                   ?? throw new NotFoundException(nameof(Tent), cmd.TentId);

        if (tent.RetreatId != cmd.RetreatId)
            throw new NotFoundException(nameof(Tent), cmd.TentId);

        // métodos/propriedades de lock
        var lockMethod   = tent.GetType().GetMethod("Lock",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var unlockMethod = tent.GetType().GetMethod("Unlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var lockedProp   = tent.GetType().GetProperty("IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var bumpMethod   = retreat.GetType().GetMethod("BumpTentsVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var versionProp  = retreat.GetType().GetProperty("TentsVersion",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var current = lockedProp is not null ? (bool)lockedProp.GetValue(tent)! : false;

        bool changed = false;

        if (cmd.Lock && !current)
        {
            if (lockMethod is not null) lockMethod.Invoke(tent, null);
            else lockedProp?.SetValue(tent, true);
            changed = true;
        }
        else if (!cmd.Lock && current)
        {
            if (unlockMethod is not null) unlockMethod.Invoke(tent, null);
            else lockedProp?.SetValue(tent, false);
            changed = true;
        }

        await tentRepo.UpdateAsync(tent, ct);

        if (changed)
            bumpMethod?.Invoke(retreat, null);

        await uow.SaveChangesAsync(ct);

        var ver = versionProp is not null ? (int)versionProp.GetValue(retreat)! : 0;
        var nowLocked = lockedProp is not null ? (bool)lockedProp.GetValue(tent)! : cmd.Lock;

        return new SetTentLockResponse(retreat.Id, tent.Id, nowLocked, ver);
    }
}
