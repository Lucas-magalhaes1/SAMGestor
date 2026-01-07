using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.GetDetail;

public sealed record GetReportDetailQuery(
    string Id, 
    int Skip = 0, 
    int Take = 0  
) : IRequest<ReportPayload?>;