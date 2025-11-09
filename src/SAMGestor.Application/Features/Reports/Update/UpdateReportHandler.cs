using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.Update;

public sealed class UpdateReportHandler : IRequestHandler<UpdateReportCommand, ReportListItemDto?>
{
    private readonly IReportCatalog _catalog;
    public UpdateReportHandler(IReportCatalog catalog) => _catalog = catalog;
    
    public Task<ReportListItemDto?> Handle(UpdateReportCommand request, CancellationToken ct)
        => _catalog.UpdateAsync(request.Id, 
            new UpdateReportRequest(request.Title, request.TemplateKey, request.DefaultParamsJson), ct);
}