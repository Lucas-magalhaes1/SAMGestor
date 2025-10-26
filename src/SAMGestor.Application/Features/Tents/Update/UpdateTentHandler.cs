using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Tents.Update;

public sealed class UpdateTentHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    IRegistrationRepository regRepo,
    IUnitOfWork uow
) : IRequestHandler<UpdateTentCommand, UpdateTentResponse>
{
    public async Task<UpdateTentResponse> Handle(UpdateTentCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        var lockedProp = retreat.GetType().GetProperty("TentsLocked",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lockedProp is not null && lockedProp.PropertyType == typeof(bool))
        {
            var isLocked = (bool)lockedProp.GetValue(retreat)!;
            if (isLocked)
                throw new BusinessRuleException("Barracas estão bloqueadas para edição neste retiro.");
        }

        var tent = await tentRepo.GetByIdAsync(cmd.TentId, ct)
                   ?? throw new NotFoundException(nameof(Tent), cmd.TentId);

        if (tent.RetreatId != cmd.RetreatId)
            throw new NotFoundException(nameof(Tent), cmd.TentId);
        if (tent.IsLocked)
            throw new BusinessRuleException("Esta barraca está bloqueada para edição.");
        
        var assigned = await regRepo.CountByTentAsync(tent.Id, ct);
        
        if (cmd.Capacity < assigned)
            throw new BusinessRuleException("Capacidade não pode ser menor que o número de ocupantes atuais.");

        var changingCategory = cmd.Category != tent.Category;
        if (changingCategory && assigned > 0)
            throw new BusinessRuleException("Não é possível alterar a categoria de uma barraca que já possui ocupantes.");
        
        if (!int.TryParse(cmd.Number, out var numberInt))
            throw new BusinessRuleException("Number inválido (deve ser numérico).");
        var number = new TentNumber(numberInt);
        
        var exists = await tentRepo.ExistsNumberAsync(cmd.RetreatId, cmd.Category, number, tent.Id, ct);
        if (exists)
            throw new BusinessRuleException("Já existe barraca com este número para a mesma categoria neste retiro.");
        
        SetValue(tent, "Number", number);
        SetValue(tent, "Category", cmd.Category);
        SetValue(tent, "Capacity", cmd.Capacity);

        if (cmd.IsActive is bool active)
            SetValue(tent, "IsActive", active);

        if (cmd.Notes is not null)
        {
            var upNotes = typeof(Tent).GetMethod("UpdateNotes",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (upNotes is not null) upNotes.Invoke(tent, new object?[] { cmd.Notes!.Trim() });
            else SetValue(tent, "Notes", cmd.Notes!.Trim());
        }

        await tentRepo.UpdateAsync(tent, ct);
        
        var bumpMethod = retreat.GetType().GetMethod("BumpTentsVersion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bumpMethod?.Invoke(retreat, null);

        await uow.SaveChangesAsync(ct);
        
        var versionProp = retreat.GetType().GetProperty("TentsVersion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var version = versionProp is not null ? (int)versionProp.GetValue(retreat)! : 0;

        return new UpdateTentResponse(tent.Id, cmd.RetreatId, version);
    }

    private static void SetValue(object obj, string prop, object? value)
    {
        var p = obj.GetType().GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        p?.SetValue(obj, value);
    }
}
