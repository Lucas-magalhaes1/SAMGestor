using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.List;

public sealed record ListReportsQuery(int Page = 1, int Limit = 10) 
    : IRequest<PaginatedResponse<ReportListItemDto>>;