namespace SAMGestor.Application.Dtos.Reports;

public sealed record CreateReportRequest(
    string Title,
    string TemplateKey,
    Guid? RetreatId = null,
    string? DefaultParamsJson = null
);

public sealed record UpdateReportRequest(
    string Title,
    string? TemplateKey = null,
    string? DefaultParamsJson = null
);