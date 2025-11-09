namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportListItemDto(
    string Id,
    string Title,
    DateTime DateCreation
);