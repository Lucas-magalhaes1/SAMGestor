namespace SAMGestor.Application.Dtos.Reports;

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Data,
    int Total,
    int Page,
    int PageLimit
);