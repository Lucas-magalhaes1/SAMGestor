using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Detail;

public sealed class GetServiceSpaceDetailHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo,
    IServiceRegistrationRepository regRepo
) : IRequestHandler<GetServiceSpaceDetailQuery, GetServiceSpaceDetailResponse>
{
    public async Task<GetServiceSpaceDetailResponse> Handle(GetServiceSpaceDetailQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var space = await spaceRepo.GetByIdAsync(q.SpaceId, ct)
                   ?? throw new NotFoundException(nameof(ServiceSpace), q.SpaceId);

        if (space.RetreatId != q.RetreatId)
            throw new BusinessRuleException("Space does not belong to this retreat.");

        var assigns = await assignRepo.ListBySpaceIdsAsync(new[] { q.SpaceId }, ct);
        var list = assigns.Where(a => a.ServiceSpaceId == q.SpaceId).ToList();

        var regIds = list.Select(a => a.ServiceRegistrationId).Distinct().ToArray();
        var regMap = await regRepo.GetMapByIdsAsync(regIds, ct);
        
        var members = list
            .Select(a =>
            {
                var r = regMap[a.ServiceRegistrationId];
                return new MemberItem(
                    r.Id, 
                    (string)r.Name, 
                    r.Email.Value, 
                    r.Cpf.Value, 
                    a.Role.ToString()
                );
            })
            .ToList();
        
        // Filtro por search
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            members = members.Where(m =>
                    m.Name.ToLowerInvariant().Contains(s) ||
                    m.Email.ToLowerInvariant().Contains(s) ||
                    m.Cpf.Contains(s))
                .ToList();
        }

        // Ordenação
        members = members
            .OrderByDescending(m => m.Role == nameof(ServiceRole.Coordinator))
            .ThenByDescending(m => m.Role == nameof(ServiceRole.Vice))
            .ThenBy(m => m.Name)
            .ToList();

        var totalCount = members.Count;

        // Paginação usando extension
        var pagedMembers = members
            .ApplyPagination(q.Skip, q.Take)
            .ToList();

        var hasCoord = list.Any(a => a.Role == ServiceRole.Coordinator);
        var hasVice  = list.Any(a => a.Role == ServiceRole.Vice);

        var view = new SpaceView(
            SpaceId: space.Id,
            Name: space.Name,
            Description: space.Description,
            IsActive: space.IsActive,
            IsLocked: space.IsLocked,
            MinPeople: space.MinPeople,
            MaxPeople: space.MaxPeople,
            HasCoordinator: hasCoord,
            HasVice: hasVice,
            Allocated: list.Count
        );

        var pagedResult = new PagedResult<MemberItem>(pagedMembers, totalCount, q.Skip, q.Take);

        return new GetServiceSpaceDetailResponse(
            Version: retreat.ServiceSpacesVersion,
            Space: view,
            Members: pagedResult
        );
    }
}
