using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Groups;

public sealed class NotifyFamilyGroupHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo,
    IEventBus bus,
    IUnitOfWork uow
) : IRequestHandler<NotifyFamilyGroupCommand, NotifyFamilyGroupResponse>
{
    public async Task<NotifyFamilyGroupResponse> Handle(NotifyFamilyGroupCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");

        if (!retreat.FamiliesLocked && !family.IsLocked)
            throw new BusinessRuleException("É necessário travar o retiro ou a família para notificar/criar o grupo.");

        // Carrega links e usa a CONTAGEM para validar completude
        var links = await familyMemberRepo.ListByFamilyAsync(family.Id, ct);
        if (links.Count < family.Capacity)
            throw new BusinessRuleException("Família incompleta.");

        if (!cmd.ForceRecreate && !string.IsNullOrWhiteSpace(family.GroupLink))
            return new NotifyFamilyGroupResponse(false, true, family.GroupVersion);

        var regsMap = await registrationRepo.GetMapByIdsAsync(links.Select(l => l.RegistrationId).Distinct(), ct);

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

        var evt = new FamilyGroupCreateRequestedV1(cmd.RetreatId, cmd.FamilyId, cmd.Channel, cmd.ForceRecreate, contacts);

        await bus.EnqueueAsync(
            type: EventTypes.FamilyGroupCreateRequestedV1,
            source: "sam.core",
            data: evt,
            traceId: null,
            ct: ct);

        family.MarkGroupCreating();
        await familyRepo.UpdateAsync(family, ct);
        await uow.SaveChangesAsync(ct);

        return new NotifyFamilyGroupResponse(true, false, family.GroupVersion);
    }
}
