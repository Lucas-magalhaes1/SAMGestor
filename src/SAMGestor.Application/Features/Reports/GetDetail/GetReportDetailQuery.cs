using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.GetDetail;

public sealed record GetReportDetailQuery(string Id, int Page = 1, int PageLimit = 0) 
    : IRequest<ReportPayload?>;