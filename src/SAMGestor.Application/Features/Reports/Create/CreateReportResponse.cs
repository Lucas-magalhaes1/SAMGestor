namespace SAMGestor.Application.Features.Reports.Create;

public sealed record CreateReportResponse(
    string Id,
    string Title,
    DateTime DateCreation
);