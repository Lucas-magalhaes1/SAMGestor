using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.GetDetail;

public sealed class GetReportDetailHandler : IRequestHandler<GetReportDetailQuery, ReportPayload?>
{
    private readonly IReportEngine _engine;
    public GetReportDetailHandler(IReportEngine engine) => _engine = engine;

    public async Task<ReportPayload?> Handle(GetReportDetailQuery request, CancellationToken ct)
    {
        try
        {
            var ctx = await _engine.BuildContextAsync(request.Id, ct);
            return await _engine.GetPayloadAsync(ctx, request.Page, request.PageLimit, ct);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }
}