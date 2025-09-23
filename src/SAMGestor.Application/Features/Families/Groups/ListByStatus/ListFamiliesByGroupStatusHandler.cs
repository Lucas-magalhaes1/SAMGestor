using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups.ListByStatus;

public sealed class ListFamiliesByGroupStatusHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo
) : IRequestHandler<ListFamiliesByGroupStatusQuery, ListFamiliesByGroupStatusResponse>
{
    public async Task<ListFamiliesByGroupStatusResponse> Handle(ListFamiliesByGroupStatusQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var families = await familyRepo.ListByRetreatAsync(q.RetreatId, ct);

        GroupStatus? filter = q.Status?.ToLowerInvariant() switch
        {
            "none"     => GroupStatus.None,
            "creating" => GroupStatus.Creating,
            "active"   => GroupStatus.Active,
            "failed"   => GroupStatus.Failed,
            null or "" => null,
            _ => null
        };

        var items = families
            .Where(f => filter == null || f.GroupStatus == filter)
            .OrderBy(f => f.Name.Value)
            .Select(f => new FamilyGroupItem(
                f.Id, (string)f.Name, f.GroupStatus.ToString(),
                f.GroupLink, f.GroupExternalId, f.GroupChannel,
                f.GroupCreatedAt, f.GroupLastNotifiedAt, f.GroupVersion
            ))
            .ToList();

        return new ListFamiliesByGroupStatusResponse(items);
    }
}