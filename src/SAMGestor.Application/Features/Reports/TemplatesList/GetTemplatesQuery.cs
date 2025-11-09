using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.TemplatesList;

public sealed record GetTemplatesSchemasQuery() : IRequest<IReadOnlyList<ReportTemplateSchemaDto>>;