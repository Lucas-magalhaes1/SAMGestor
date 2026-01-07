using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public sealed class ListRetreatsHandler
    : IRequestHandler<ListRetreatsQuery, PagedResult<RetreatDto>>
{
    private readonly IRetreatRepository _repo;

    public ListRetreatsHandler(IRetreatRepository repo) => _repo = repo;

    public async Task<PagedResult<RetreatDto>> Handle(
        ListRetreatsQuery query,
        CancellationToken ct)
    {
        var skip = Math.Max(0, query.Skip);
        var take = query.Take;
        
        var (retreats, totalCount) = await _repo.ListAsync(query.Skip, query.Take, ct);

        var items = retreats.Select(r => new RetreatDto(
                r.Id,
                (string)r.Name,
                r.Edition,
                r.StartDate,
                r.EndDate))
            .ToList();

        return new PagedResult<RetreatDto>(items, totalCount, query.Skip, query.Take);
    }
}