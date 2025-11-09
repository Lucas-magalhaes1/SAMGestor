using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IReportEngine
{
    Task<ReportContext> BuildContextAsync(string reportId, CancellationToken ct);
    Task<ReportPayload> GetPayloadAsync(ReportContext ctx, int page, int pageLimit, CancellationToken ct);
}