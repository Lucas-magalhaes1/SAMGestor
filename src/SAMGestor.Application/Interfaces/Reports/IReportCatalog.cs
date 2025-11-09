using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

/// Operações de catálogo (metadados).
public interface IReportCatalog
{
    Task<PaginatedResponse<ReportListItemDto>> ListAsync(int page, int limit, CancellationToken ct);
    Task<string> CreateAsync(CreateReportRequest request, CancellationToken ct);
    Task<ReportListItemDto?> UpdateAsync(string id, UpdateReportRequest request, CancellationToken ct);
    Task<(bool ok, string id)> DeleteAsync(string id, CancellationToken ct);
    Task<PaginatedResponse<ReportListItemDto>> ListByRetreatAsync(Guid retreatId, int page, int limit, CancellationToken ct);
}