using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.List;

public sealed record ListReportsQuery(int Skip = 0, int Take = 10) 
    : IRequest<PagedResult<ReportListItemDto>>;