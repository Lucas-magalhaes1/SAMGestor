using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Registrations.GetAll;

public sealed class GetAllRegistrationsHandler(IRegistrationRepository repo, IStorageService storage)
    : IRequestHandler<GetAllRegistrationsQuery, PagedResult<RegistrationDto>>
{
    public async Task<PagedResult<RegistrationDto>> Handle(GetAllRegistrationsQuery query, CancellationToken ct)
    {
        // 1. Busca todas do banco (sem paginação ainda pois filtros são em memória)
        var list = await repo.ListAsync(query.retreatId, query.status, region: null, ct: ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var filtered = list.AsEnumerable();

        // 2. Filtros em memória
        if (!string.IsNullOrWhiteSpace(query.status) &&
            Enum.TryParse<RegistrationStatus>(query.status, true, out var st))
            filtered = filtered.Where(r => r.Status == st);

        if (query.gender is not null)
            filtered = filtered.Where(r => r.Gender == query.gender);

        if (query.minAge is not null)
            filtered = filtered.Where(r => r.GetAgeOn(today) >= query.minAge);

        if (query.maxAge is not null)
            filtered = filtered.Where(r => r.GetAgeOn(today) <= query.maxAge);

        if (!string.IsNullOrWhiteSpace(query.city))
        {
            var city = query.city.Trim().ToLowerInvariant();
            filtered = filtered.Where(r => (r.City ?? string.Empty).ToLowerInvariant().Contains(city));
        }

        if (query.state is not null)
            filtered = filtered.Where(r => r.State == query.state);

        if (!string.IsNullOrWhiteSpace(query.search))
        {
            var s = query.search.Trim().ToLowerInvariant();
            filtered = filtered.Where(r =>
                ((string)r.Name).ToLowerInvariant().Contains(s) ||
                r.Cpf.Value.Contains(s) ||
                r.Email.Value.ToLowerInvariant().Contains(s));
        }

        if (query.hasPhoto is not null)
        {
            var want = query.hasPhoto.Value;
            filtered = filtered.Where(r => (r.PhotoStorageKey != null || r.PhotoUrl != null) == want);
        }

        // 3. Total APÓS filtros
        var totalCount = filtered.Count();

        // 4. Paginação em memória + projeção
        var items = filtered
            .ApplyPagination(query.skip, query.take)
            .Select(r =>
            {
                var photoUrl = r.PhotoUrl?.Value;
                if (string.IsNullOrWhiteSpace(photoUrl) && !string.IsNullOrWhiteSpace(r.PhotoStorageKey))
                    photoUrl = storage.GetPublicUrl(r.PhotoStorageKey);

                return new RegistrationDto(
                    r.Id,
                    (string)r.Name,
                    r.Cpf.Value,
                    r.Status.ToString(),
                    r.Gender.ToString(),
                    r.GetAgeOn(today),
                    r.City,
                    r.State?.ToString(),
                    r.RegistrationDate,
                    photoUrl
                );
            })
            .ToList();

        return new PagedResult<RegistrationDto>(items, totalCount, query.skip, query.take);
    }
}
