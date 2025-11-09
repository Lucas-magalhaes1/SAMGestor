using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.ListByRetreat;

public sealed record ListReportsByRetreatQuery(Guid RetreatId, int Page = 1, int Limit = 10)
    : IRequest<PaginatedResponse<ReportListItemDto>>;