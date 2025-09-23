using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups.Resend;

public sealed class ResendFamilyGroupHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IEventBus bus
) : IRequestHandler<ResendFamilyGroupCommand, ResendFamilyGroupResponse>
{
    public async Task<ResendFamilyGroupResponse> Handle(ResendFamilyGroupCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");

        // precisa já ter link criado
        if (string.IsNullOrWhiteSpace(family.GroupLink))
            return new ResendFamilyGroupResponse(false, "NO_GROUP_LINK");

        // carrega membros da família para montar contatos
        var links = await familyMemberRepo.ListByFamilyAsync(family.Id, ct);
        if (links.Count == 0)
            return new ResendFamilyGroupResponse(false, "NO_MEMBERS");

        var regsMap = await registrationRepo.GetMapByIdsAsync(links.Select(l => l.RegistrationId).Distinct(), ct);

        var members = links
            .OrderBy(l => l.Position)
            .Select(l =>
            {
                var r = regsMap[l.RegistrationId];
                return new FamilyGroupNotifyRequestedV1.MemberContact(
                    r.Id, (string)r.Name, r.Email?.Value, r.Phone
                );
            })
            .ToList();

        var evt = new FamilyGroupNotifyRequestedV1(
            RetreatId: cmd.RetreatId,
            FamilyId:  cmd.FamilyId,
            GroupLink: family.GroupLink!,
            Members:   members
        );

        await bus.EnqueueAsync(
            type: EventTypes.FamilyGroupNotifyRequestedV1,
            source: "sam.core",
            data: evt,
            traceId: null,
            ct: ct);

        return new ResendFamilyGroupResponse(true, null);
    }
}
