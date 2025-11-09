using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.List;

public sealed class ListReportsHandler : IRequestHandler<ListReportsQuery, PaginatedResponse<ReportListItemDto>>
{
    private readonly IReportCatalog _catalog;
    public ListReportsHandler(IReportCatalog catalog) => _catalog = catalog;

    public Task<PaginatedResponse<ReportListItemDto>> Handle(ListReportsQuery request, CancellationToken ct)
        => _catalog.ListAsync(request.Page, request.Limit, ct);
}