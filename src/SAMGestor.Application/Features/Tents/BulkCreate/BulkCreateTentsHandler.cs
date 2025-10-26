using System.Reflection;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Tents.BulkCreate;

public sealed class BulkCreateTentsHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    IUnitOfWork uow
) : IRequestHandler<BulkCreateTentsCommand, BulkCreateTentsResponse>
{
    public async Task<BulkCreateTentsResponse> Handle(BulkCreateTentsCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        // lock global (se existir no domínio)
        var lockedProp = retreat.GetType().GetProperty("TentsLocked",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lockedProp is not null && lockedProp.PropertyType == typeof(bool))
        {
            var isLocked = (bool)lockedProp.GetValue(retreat)!;
            if (isLocked)
                throw new BusinessRuleException("Barracas estão bloqueadas para edição neste retiro.");
        }

        // carrega barracas existentes para checagem de duplicidade
        var existing = await tentRepo.ListByRetreatAsync(cmd.RetreatId, ct: ct);
        var existingKeys = new HashSet<(TentCategory cat, int num)>(
            existing.Select(t => (t.Category, t.Number.Value))
        );

        var toCreate = new List<(BulkCreateTentItemDto dto, Tent entity)>();
        var errors   = new List<BulkCreateTentError>();

        // Evitar duplicidade dentro do próprio processamento (Category, Number)
        var seen = new HashSet<(TentCategory cat, int num)>();

        foreach (var item in cmd.Items)
        {
            if (!int.TryParse(item.Number, out var numberInt))
            {
                errors.Add(new BulkCreateTentError(
                    "INVALID_NUMBER", "Number inválido (deve ser numérico).",
                    item.Number, item.Category));
                continue;
            }

            var key = (item.Category, numberInt);

            // duplicado no payload (já validado no validator)
            if (!seen.Add(key))
            {
                errors.Add(new BulkCreateTentError(
                    "DUPLICATE_PAYLOAD", "Número duplicado no payload para a mesma categoria.",
                    item.Number, item.Category));
                continue;
            }
            
            if (existingKeys.Contains(key))
            {
                errors.Add(new BulkCreateTentError(
                    "DUPLICATE_EXISTING", "Já existe barraca com este número para a mesma categoria neste retiro.",
                    item.Number, item.Category));
                continue;
            }

            var entity = new Tent(
                number: new TentNumber(numberInt),
                category: item.Category,
                capacity: item.Capacity,
                retreatId: cmd.RetreatId
            );

            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                var upNotes = typeof(Tent).GetMethod("UpdateNotes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (upNotes is not null) upNotes.Invoke(entity, new object?[] { item.Notes!.Trim() });
                else
                {
                    var prop = typeof(Tent).GetProperty("Notes",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    prop?.SetValue(entity, item.Notes!.Trim());
                }
            }

            toCreate.Add((item, entity));
        }

        // persistir
        foreach (var (_, entity) in toCreate)
            await tentRepo.AddAsync(entity, ct);

        if (toCreate.Count > 0)
        {
            var bump = retreat.GetType().GetMethod("BumpTentsVersion",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bump?.Invoke(retreat, null);
        }

        await uow.SaveChangesAsync(ct);

        var views = toCreate
            .Select(t => new BulkCreatedTentView(
                TentId: t.entity.Id,
                Number: t.entity.Number.Value.ToString(),
                Category: t.entity.Category,
                Capacity: t.entity.Capacity,
                IsActive: t.entity.IsActive,
                IsLocked: t.entity.IsLocked,
                Notes: t.entity.Notes
            ))
            .OrderBy(v => v.Category)
            .ThenBy(v => v.Number, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new BulkCreateTentsResponse(
            RetreatId: cmd.RetreatId,
            Created: toCreate.Count,
            Skipped: errors.Count,
            Tents: views,
            Errors: errors
        );
    }
}
