using MediatR;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.List;

public sealed class ListTentsHandler(
    ITentRepository tentRepo,
    IRegistrationRepository regRepo
) : IRequestHandler<ListTentsQuery, IReadOnlyList<TentListItem>>
{
    public async Task<IReadOnlyList<TentListItem>> Handle(ListTentsQuery q, CancellationToken ct)
    {
        var tents = await tentRepo.ListByRetreatAsync(q.RetreatId, ct: ct);

        // NÃO use 'ct' como nome do pattern var para não sombrear o CancellationToken
        if (q.Category is TentCategory cat)
            tents = tents.Where(t => t.Category == cat).ToList();

        if (q.Active is bool active)
            tents = tents.Where(t => t.IsActive == active).ToList();

        if (tents.Count == 0)
            return Array.Empty<TentListItem>();

        var tentIds  = tents.Select(t => t.Id).ToArray();
        var countMap = await regRepo.GetAssignedCountMapByTentIdsAsync(q.RetreatId, tentIds, ct);

        var result = tents
            .Select(t =>
            {
                countMap.TryGetValue(t.Id, out var assigned);
                return new TentListItem(
                    TentId:   t.Id,
                    Number:   t.Number.Value.ToString(),  // ← sem ?.
                    Category: t.Category,
                    Capacity: t.Capacity,
                    IsActive: t.IsActive,
                    IsLocked: t.IsLocked,
                    Notes:    t.Notes,
                    AssignedCount: assigned
                );
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result;
    }
}