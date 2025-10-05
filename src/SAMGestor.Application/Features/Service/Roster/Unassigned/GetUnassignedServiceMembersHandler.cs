using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Roster.Unassigned;

public sealed class GetUnassignedServiceMembersHandler(
    IRetreatRepository retreatRepo,
    IServiceRegistrationRepository regRepo,
    IServiceAssignmentRepository assignRepo,
    IServiceSpaceRepository spaceRepo
) : IRequestHandler<GetUnassignedServiceMembersQuery, GetUnassignedServiceMembersResponse>
{
    public async Task<GetUnassignedServiceMembersResponse> Handle(GetUnassignedServiceMembersQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        // todos os servidores (Enabled) do retiro
        var regs = await regRepo.ListByRetreatAsync(q.RetreatId, ct);

        // remover já alocados
        var links = await assignRepo.ListByRetreatAsync(q.RetreatId, ct);
        var assigned = links.Select(l => l.ServiceRegistrationId).ToHashSet();
        var pool = regs.Where(r => !assigned.Contains(r.Id)).ToList();

        // filtros opcionais (mesma ideia do Families)
        if (!string.IsNullOrWhiteSpace(q.Gender) && Enum.TryParse<Gender>(q.Gender, true, out var g))
            pool = pool.Where(r => r.Gender == g).ToList();

        if (!string.IsNullOrWhiteSpace(q.City))
        {
            var city = q.City.Trim().ToLowerInvariant();
            pool = pool.Where(r => (r.City ?? string.Empty).Trim().ToLowerInvariant() == city).ToList();
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            pool = pool.Where(r =>
                ((string)r.Name).ToLowerInvariant().Contains(s)
                || r.Email.Value.ToLowerInvariant().Contains(s)
                || r.Cpf.Value.Contains(s)
            ).ToList();
        }

        // map de nomes dos espaços para exibir a preferência
        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        var spaceNameById = spaces.ToDictionary(x => x.Id, x => x.Name);

        var items = pool
            .OrderBy(r => r.Name.Value)
            .Select(r =>
            {
                var prefId = r.PreferredSpaceId;
                var prefName = prefId.HasValue && spaceNameById.TryGetValue(prefId.Value, out var nm) ? nm : null;
                return new UnassignedItem(
                    r.Id, (string)r.Name, r.City, r.Email.Value, r.Cpf.Value,
                    prefId, prefName
                );
            })
            .ToList();

        return new GetUnassignedServiceMembersResponse(
            Version: retreat.ServiceSpacesVersion,
            Items: items
        );
    }
}
