using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.List;

public sealed class ListReportsHandler : IRequestHandler<ListReportsQuery, PagedResult<ReportListItemDto>>
{
    private readonly IReportCatalog _catalog;
    public ListReportsHandler(IReportCatalog catalog) => _catalog = catalog;

    public Task<PagedResult<ReportListItemDto>> Handle(ListReportsQuery request, CancellationToken ct)
        => _catalog.ListAsync(request.Skip, request.Take, ct);
}