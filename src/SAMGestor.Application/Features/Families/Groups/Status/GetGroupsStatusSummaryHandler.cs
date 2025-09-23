using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups.Status;

public sealed class GetGroupsStatusSummaryHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo
) : IRequestHandler<GetGroupsStatusSummaryQuery, GetGroupsStatusSummaryResponse>
{
    public async Task<GetGroupsStatusSummaryResponse> Handle(GetGroupsStatusSummaryQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var families = await familyRepo.ListByRetreatAsync(q.RetreatId, ct);

        int none=0, creating=0, active=0, failed=0;
        foreach (var f in families)
        {
            switch (f.GroupStatus)
            {
                case SAMGestor.Domain.Enums.GroupStatus.None:     none++;     break;
                case SAMGestor.Domain.Enums.GroupStatus.Creating: creating++; break;
                case SAMGestor.Domain.Enums.GroupStatus.Active:   active++;   break;
                case SAMGestor.Domain.Enums.GroupStatus.Failed:   failed++;   break;
            }
        }

        return new GetGroupsStatusSummaryResponse(
            TotalFamilies: families.Count,
            None: none, Creating: creating, Active: active, Failed: failed
        );
    }
}