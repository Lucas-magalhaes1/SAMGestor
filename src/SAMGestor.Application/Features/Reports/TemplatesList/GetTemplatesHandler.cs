using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.TemplatesList;

public sealed class GetTemplatesSchemasHandler 
    : IRequestHandler<GetTemplatesSchemasQuery, IReadOnlyList<ReportTemplateSchemaDto>>
{
    private readonly IReportTemplateRegistry _registry;
    public GetTemplatesSchemasHandler(IReportTemplateRegistry registry) => _registry = registry;

    public Task<IReadOnlyList<ReportTemplateSchemaDto>> Handle(GetTemplatesSchemasQuery request, CancellationToken ct)
        => Task.FromResult(_registry.ListSchemas());
}