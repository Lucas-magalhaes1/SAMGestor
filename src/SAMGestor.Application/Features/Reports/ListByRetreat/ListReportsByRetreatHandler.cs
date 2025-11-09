using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.ListByRetreat;

public sealed class ListReportsByRetreatHandler
    : IRequestHandler<ListReportsByRetreatQuery, PaginatedResponse<ReportListItemDto>>
{
    private readonly IReportCatalog _catalog;
    public ListReportsByRetreatHandler(IReportCatalog catalog) => _catalog = catalog;

    public Task<PaginatedResponse<ReportListItemDto>> Handle(ListReportsByRetreatQuery request, CancellationToken ct)
        => _catalog.ListByRetreatAsync(request.RetreatId, request.Page, request.Limit, ct);
}