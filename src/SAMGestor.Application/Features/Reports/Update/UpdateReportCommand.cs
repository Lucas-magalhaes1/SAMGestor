using MediatR;
using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Features.Reports.Update;

public sealed record UpdateReportCommand(
    string Id,
    string Title,
    string? TemplateKey = null,
    string? DefaultParamsJson = null
) : IRequest<ReportListItemDto?>;