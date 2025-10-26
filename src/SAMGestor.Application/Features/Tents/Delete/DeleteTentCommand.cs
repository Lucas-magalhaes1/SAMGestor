using MediatR;

namespace SAMGestor.Application.Features.Tents.Delete;

public sealed record DeleteTentCommand(
    Guid RetreatId,
    Guid TentId
) : IRequest<DeleteTentResponse>;