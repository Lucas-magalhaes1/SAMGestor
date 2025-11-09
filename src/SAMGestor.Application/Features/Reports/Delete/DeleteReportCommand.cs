using MediatR;

namespace SAMGestor.Application.Features.Reports.Delete;

public sealed record DeleteReportCommand(string Id) : IRequest<bool>;