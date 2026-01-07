using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.ListByRetreat;

public sealed record ListReportsByRetreatQuery(Guid RetreatId, int Skip = 0, int Take = 10)
    : IRequest<PagedResult<ReportListItemDto>>;