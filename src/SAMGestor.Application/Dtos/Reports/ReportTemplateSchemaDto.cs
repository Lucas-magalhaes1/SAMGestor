namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportTemplateSchemaDto(
    string Key,
    string DefaultTitle,
    IReadOnlyList<ColumnDef> Columns,
    IReadOnlyList<string> SummaryKeys,
    bool SupportsPaging,
    int DefaultPageLimit
);