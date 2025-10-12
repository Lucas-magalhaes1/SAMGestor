using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.Confirmed;

public sealed class GetConfirmedServiceRegistrationsHandler(
    IRetreatRepository retreatRepo,
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo
) : IRequestHandler<GetConfirmedServiceRegistrationsQuery, IReadOnlyList<GetConfirmedServiceRegistrationsResponse>>
{
    public async Task<IReadOnlyList<GetConfirmedServiceRegistrationsResponse>> Handle(
        GetConfirmedServiceRegistrationsQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        var spaceMap = spaces.ToDictionary(s => s.Id, s => s);

        
        var regsAll = await regRepo.ListByRetreatAsync(q.RetreatId, ct);
        var regs = regsAll
            .Where(r => r.Status == ServiceRegistrationStatus.Confirmed)
            .OrderBy(r => (string)r.Name)
            .ToList();

        var spaceIds = spaces.Select(s => s.Id).ToArray();
        var assigns = await assignRepo.ListBySpaceIdsAsync(spaceIds, ct);
        var assignByReg = assigns.ToDictionary(a => a.ServiceRegistrationId, a => a);
        
        var list = new List<GetConfirmedServiceRegistrationsResponse>(regs.Count);
        foreach (var r in regs)
        {
            spaceMap.TryGetValue(r.PreferredSpaceId ?? Guid.Empty, out var prefSpace);

            ServiceAssignment? a = assignByReg.TryGetValue(r.Id, out var link) ? link : null;
            ServiceRole? role = a?.Role;

            string? assignedName = null;
            if (a is not null && spaceMap.TryGetValue(a.ServiceSpaceId, out var aspace))
                assignedName = aspace.Name;

            list.Add(new GetConfirmedServiceRegistrationsResponse(
                RegistrationId: r.Id,
                Name: (string)r.Name,
                Email: r.Email.Value,
                Phone: r.Phone,
                PreferredSpaceId: r.PreferredSpaceId,
                PreferredSpaceName: prefSpace?.Name,
                AssignedSpaceId: a?.ServiceSpaceId,
                AssignedSpaceName: assignedName,
                AssignedRole: role
            ));
        }

        return list;
    }
}
