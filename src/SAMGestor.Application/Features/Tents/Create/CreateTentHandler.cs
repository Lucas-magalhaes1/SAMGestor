using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Tents.Create;

public sealed class CreateTentHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateTentCommand, CreateTentResponse>
{
    public async Task<CreateTentResponse> Handle(CreateTentCommand cmd, CancellationToken ct)
    {
        // 1) Retiro precisa existir
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        // 2) Checa lock global (se exposto no domínio)
        //    Se não tiver TentsLocked, pode remover este bloco.
        var lockedProp = retreat.GetType().GetProperty("TentsLocked",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lockedProp is not null && lockedProp.PropertyType == typeof(bool))
        {
            var isLocked = (bool)lockedProp.GetValue(retreat)!;
            if (isLocked)
                throw new InvalidOperationException("As barracas estão travadas para este retiro.");
        }

        // 3) Converter o Number (string -> int)
        if (!int.TryParse(cmd.Number, out var numberInt))
            throw new InvalidOperationException("Number inválido (deve ser numérico).");

        var number = new TentNumber(numberInt);

        // Reforço: unicidade
        var exists = await tentRepo.ExistsNumberAsync(cmd.RetreatId, cmd.Category, number, null, ct);
        if (exists)
            throw new InvalidOperationException("Já existe barraca com este número para a mesma categoria neste retiro.");

        // 4) Criar entidade
        var tent = new Tent(
            number: number,
            category: cmd.Category,
            capacity: cmd.Capacity,
            retreatId: cmd.RetreatId
        );

        // 5) Notas (usa método se existir; senão, seta a propriedade via reflection)
        if (!string.IsNullOrWhiteSpace(cmd.Notes))
        {
            var method = typeof(Tent).GetMethod("UpdateNotes",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method is not null)
            {
                method.Invoke(tent, new object?[] { cmd.Notes!.Trim() });
            }
            else
            {
                var prop = typeof(Tent).GetProperty("Notes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                prop?.SetValue(tent, cmd.Notes!.Trim());
            }
        }

        await tentRepo.AddAsync(tent, ct);

        // 6) Bump na versão de barracas (se existir no domínio)
        var bumpMethod = retreat.GetType().GetMethod("BumpTentsVersion",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bumpMethod?.Invoke(retreat, null);

        await uow.SaveChangesAsync(ct);

        return new CreateTentResponse(tent.Id, cmd.RetreatId);
    }
}
