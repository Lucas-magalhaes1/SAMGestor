using MediatR;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public sealed class ListRetreatsHandler
    : IRequestHandler<ListRetreatsQuery, ListRetreatsResponse>
{
    private readonly IRetreatRepository _repo;

    public ListRetreatsHandler(IRetreatRepository repo) => _repo = repo;

    public async Task<ListRetreatsResponse> Handle(
        ListRetreatsQuery query,
        CancellationToken ct)
    {
        var skip = Math.Max(query.Skip, 0);
        var take = query.Take <= 0 ? 20 : query.Take;

        var total   = await _repo.CountAsync(ct);
        var retreats = await _repo.ListAsync(skip, take, ct);

        var items = retreats.Select(r => new RetreatDto(
                r.Id,
                (string)r.Name,
                r.Edition,
                r.StartDate,
                r.EndDate))
            .ToList();

        return new ListRetreatsResponse(items, total, skip, take);
    }
}