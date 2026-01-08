namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportContext(
    string ReportId,
    string Title,
    string TemplateKey,
    Guid? RetreatId,
    string? DefaultParamsJson,
    string? RetreatName      
);