using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.ListByRetreat;

public sealed class ListReportsByRetreatHandler
    : IRequestHandler<ListReportsByRetreatQuery, PagedResult<ReportListItemDto>>
{
    private readonly IReportCatalog _catalog;
    public ListReportsByRetreatHandler(IReportCatalog catalog) => _catalog = catalog;

    public Task<PagedResult<ReportListItemDto>> Handle(ListReportsByRetreatQuery request, CancellationToken ct)
        => _catalog.ListByRetreatAsync(request.RetreatId, request.Skip, request.Take, ct);
}