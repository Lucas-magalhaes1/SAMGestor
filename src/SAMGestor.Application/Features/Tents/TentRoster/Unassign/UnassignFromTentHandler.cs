using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Application.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.Unassign;

public sealed class UnassignFromTentHandler(
    IRetreatRepository retreatRepo,
    ITentRepository tentRepo,
    ITentAssignmentRepository assignRepo,
    IUnitOfWork uow
) : IRequestHandler<UnassignFromTentCommand, UnassignFromTentResponse>
{
    public async Task<UnassignFromTentResponse> Handle(UnassignFromTentCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.TentsLocked)
            throw new BusinessRuleException("Barracas estão bloqueadas para edição.");

        // Coleta assignments atuais dessas registrations
        var assignments = new List<TentAssignment>(cmd.RegistrationIds.Count);
        var affectedTentIds = new HashSet<Guid>();

        foreach (var regId in cmd.RegistrationIds.Distinct())
        {
            var a = await assignRepo.GetByRegistrationIdAsync(regId, ct);
            if (a is not null)
            {
                assignments.Add(a);
                affectedTentIds.Add(a.TentId);
            }
        }

        if (assignments.Count == 0)
            return new UnassignFromTentResponse(retreat.TentsVersion, 0, Array.Empty<Guid>());
        
        var impactedTents = affectedTentIds.Count > 0
            ? await tentRepo.ListByRetreatAsync(cmd.RetreatId, null, null, ct)
            : new List<Tent>();

        var impactedMap = impactedTents.Where(t => affectedTentIds.Contains(t.Id))
                                       .ToDictionary(t => t.Id, t => t);

        var lockedTent = impactedMap.Values.FirstOrDefault(t => t.IsLocked);
        if (lockedTent is not null)
            throw new BusinessRuleException($"Barraca '{lockedTent.Number.Value}' está bloqueada para edição.");
        
        await assignRepo.RemoveRangeAsync(assignments, ct);
        
        retreat.BumpTentsVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new UnassignFromTentResponse(
            Version: retreat.TentsVersion,
            Removed: assignments.Count,
            AffectedTentIds: affectedTentIds.ToArray()
        );
    }
}
