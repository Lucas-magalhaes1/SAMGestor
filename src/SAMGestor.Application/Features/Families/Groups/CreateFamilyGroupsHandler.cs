using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups;

public sealed class CreateFamilyGroupsBulkHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IEventBus bus,
    IUnitOfWork uow
) : IRequestHandler<CreateFamilyGroupsCommand, CreateFamilyGroupsResponse>
{
    public async Task<CreateFamilyGroupsResponse> Handle(CreateFamilyGroupsCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (!retreat.FamiliesLocked)
            throw new BusinessRuleException("Retiro deve estar travado para criar grupos em massa.");

        var families = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        if (families.Count == 0)
            return new CreateFamilyGroupsResponse(0, 0, 0, cmd.Channel);

        // Pré-carrega vínculos de membros por família
        var linksByFamily = await familyMemberRepo.ListByFamilyIdsAsync(families.Select(f => f.Id), ct);

        // Pré-carrega registros (para nome/email/phone)
        var regIds = linksByFamily.Values.SelectMany(v => v).Select(l => l.RegistrationId).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(regIds, ct);

        int queued = 0, skipped = 0;

        foreach (var fam in families)
        {
            // COMPLETUDE baseada na contagem carregada do repo de links (não em fam.IsComplete)
            var links = linksByFamily.TryGetValue(fam.Id, out var list) ? list : new List<FamilyMember>();
            if (links.Count < fam.Capacity) { skipped++; continue; }

            if (!cmd.ForceRecreate && !string.IsNullOrWhiteSpace(fam.GroupLink))
            {
                skipped++;
                continue;
            }

            // Monta contatos (email/phone podem estar vazios; ainda assim seguimos)
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

            if (!cmd.DryRun)
            {
                var evt = new FamilyGroupCreateRequestedV1(
                    RetreatId: cmd.RetreatId,
                    FamilyId:  fam.Id,
                    Channel:   cmd.Channel,
                    ForceRecreate: cmd.ForceRecreate,
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
            }

            queued++;
        }

        if (!cmd.DryRun) await uow.SaveChangesAsync(ct);

        return new CreateFamilyGroupsResponse(
            TotalFamilies: families.Count,
            Queued: queued,
            Skipped: skipped,
            Channel: cmd.Channel
        );
    }
}
