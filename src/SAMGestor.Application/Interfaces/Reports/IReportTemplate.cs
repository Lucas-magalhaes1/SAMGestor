using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

/// Cada relatório concreto implementa esta interface.
public interface IReportTemplate
{
    string Key { get; }
    string DefaultTitle { get; }

    Task<ReportPayload> GetDataAsync(
        ReportContext ctx,
        int page,
        int pageLimit,
        CancellationToken ct
    );
}