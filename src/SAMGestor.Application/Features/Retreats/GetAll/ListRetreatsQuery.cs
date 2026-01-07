using MediatR;
using SAMGestor.Application.Common.Pagination;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public record ListRetreatsQuery(int Skip = 0, int Take = 20)
    : IRequest<PagedResult<RetreatDto>>;

public record RetreatDto(
    Guid     Id,
    string   Name,
    string   Edition,
    DateOnly StartDate,
    DateOnly EndDate);