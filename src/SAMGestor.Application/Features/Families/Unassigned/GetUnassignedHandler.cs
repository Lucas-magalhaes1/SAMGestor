using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Unassigned;

public sealed class GetUnassignedHandler(
    IRegistrationRepository regRepo,
    IFamilyMemberRepository linkRepo
) : IRequestHandler<GetUnassignedQuery, GetUnassignedResponse>
{
    public async Task<GetUnassignedResponse> Handle(GetUnassignedQuery q, CancellationToken ct)
    {
        // 1) Pega confirmados & enabled do retiro
        var confirmed = await regRepo.ListAsync(q.RetreatId, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, ct);
        var payConf   = await regRepo.ListAsync(q.RetreatId, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, ct);

        var pool = confirmed.Concat(payConf)
            .Where(r => r.Enabled)
            .DistinctBy(r => r.Id)
            .ToList();

        // 2) Remove os que já estão alocados
        var links = await linkRepo.ListByRetreatAsync(q.RetreatId, ct);
        var assigned = links.Select(l => l.RegistrationId).ToHashSet();
        pool = pool.Where(r => !assigned.Contains(r.Id)).ToList();

        // 3) Filtros opcionais
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

        var items = pool
            .OrderBy(r => r.Name.Value)
            .Select(r => new UnassignedMemberDto(r.Id, (string)r.Name, r.Gender.ToString(), r.City, r.Email.Value))
            .ToList();

        return new GetUnassignedResponse(items);
    }
}
