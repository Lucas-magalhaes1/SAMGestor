using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups.RetryFailed;

public sealed class RetryFailedGroupsHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IEventBus bus,
    IUnitOfWork uow
) : IRequestHandler<RetryFailedGroupsCommand, RetryFailedGroupsResponse>
{
    public async Task<RetryFailedGroupsResponse> Handle(RetryFailedGroupsCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var families = (await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct))
            .Where(f => f.GroupStatus == GroupStatus.Failed)
            .ToList();

        if (families.Count == 0)
            return new RetryFailedGroupsResponse(0, 0, 0);
        
        var linksByFamily = await familyMemberRepo.ListByFamilyIdsAsync(families.Select(f => f.Id), ct);
        var regIds = linksByFamily.Values.SelectMany(v => v).Select(l => l.RegistrationId).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(regIds, ct);

        int queued = 0, skipped = 0;
        foreach (var fam in families)
        {
            var links = linksByFamily.TryGetValue(fam.Id, out var list) ? list : new List<FamilyMember>();
            if (links.Count < fam.Capacity) { skipped++; continue; }

            var contacts = links
                .OrderBy(l => l.Position)
                .Select(l =>
                {
                    var r = regsMap[l.RegistrationId];
                    return new FamilyGroupCreateRequestedV1.MemberContact(
                        r.Id, (string)r.Name, r.Email?.Value, r.Phone
                    );
                })
                .ToList();

            var evt = new FamilyGroupCreateRequestedV1(
                RetreatId: cmd.RetreatId,
                FamilyId:  fam.Id,
                ForceRecreate: true,
                Members:   contacts
            );

            await bus.EnqueueAsync(
                type: EventTypes.FamilyGroupCreateRequestedV1,
                source: "sam.core",
                data: evt,
                traceId: null,
                ct: ct);

            fam.MarkGroupCreating();
            await familyRepo.UpdateAsync(fam, ct);
            queued++;
        }

        await uow.SaveChangesAsync(ct);
        return new RetryFailedGroupsResponse(
            TotalFailed: families.Count, Queued: queued, Skipped: skipped
        );
    }
}
