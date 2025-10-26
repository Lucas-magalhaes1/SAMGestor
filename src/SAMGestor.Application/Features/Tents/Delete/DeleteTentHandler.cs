using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.Delete;

public sealed class DeleteTentHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    IRegistrationRepository regRepo,
    IUnitOfWork uow
) : IRequestHandler<DeleteTentCommand, DeleteTentResponse> 
{
    public async Task<DeleteTentResponse> Handle(DeleteTentCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        var lockedProp = retreat.GetType().GetProperty("TentsLocked",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lockedProp is not null && (bool)lockedProp.GetValue(retreat)!)
            throw new BusinessRuleException("Barracas estão bloqueadas para edição neste retiro.");

        var tent = await tentRepo.GetByIdAsync(cmd.TentId, ct)
                   ?? throw new NotFoundException(nameof(Tent), cmd.TentId);

        if (tent.RetreatId != cmd.RetreatId)
            throw new NotFoundException(nameof(Tent), cmd.TentId);

        if (tent.IsLocked)
            throw new BusinessRuleException("Esta barraca está bloqueada e não pode ser removida.");

        var assigned = await regRepo.CountByTentAsync(tent.Id, ct);
        if (assigned > 0)
            throw new BusinessRuleException("Não é possível remover barraca com ocupantes.");
        
        await tentRepo.DeleteAsync(tent, ct);

        var bumpMethod = retreat.GetType().GetMethod("BumpTentsVersion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bumpMethod?.Invoke(retreat, null);

        await uow.SaveChangesAsync(ct);

        var versionProp = retreat.GetType().GetProperty("TentsVersion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var version = versionProp is not null ? (int)versionProp.GetValue(retreat)! : 0;

        return new DeleteTentResponse(cmd.RetreatId, cmd.TentId, version);
    }
}
