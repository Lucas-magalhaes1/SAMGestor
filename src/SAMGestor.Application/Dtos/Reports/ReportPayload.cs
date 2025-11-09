namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportPayload(
    ReportHeader report,
    IReadOnlyList<ColumnDef> columns,
    IReadOnlyList<IDictionary<string, object?>> data,
    IDictionary<string, object?> summary,
    int total,
    int page,
    int pageLimit
);