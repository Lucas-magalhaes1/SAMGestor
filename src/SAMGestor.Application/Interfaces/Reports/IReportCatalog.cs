using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IReportCatalog
{
    Task<PagedResult<ReportListItemDto>> ListAsync(int skip, int take, CancellationToken ct);
    
    Task<PagedResult<ReportListItemDto>> ListByRetreatAsync(Guid retreatId, int skip, int take, CancellationToken ct);
    
    Task<string> CreateAsync(CreateReportRequest request, CancellationToken ct);
    
    Task<ReportListItemDto?> UpdateAsync(string id, UpdateReportRequest request, CancellationToken ct);
    
    Task<(bool ok, string id)> DeleteAsync(string id, CancellationToken ct);
}