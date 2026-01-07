using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IReportTemplate
{
    string Key { get; }
    string DefaultTitle { get; }

    Task<ReportPayload> GetDataAsync(
        ReportContext ctx,
        int skip,
        int take,
        CancellationToken ct
    );
}