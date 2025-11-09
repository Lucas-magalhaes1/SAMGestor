using MediatR;


namespace SAMGestor.Application.Features.Reports.Create;

public sealed record CreateReportCommand(
    string Title,
    string TemplateKey,
    Guid? RetreatId = null,
    string? DefaultParamsJson = null
) : IRequest<CreateReportResponse>;