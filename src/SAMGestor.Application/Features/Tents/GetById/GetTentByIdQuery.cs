using MediatR;

namespace SAMGestor.Application.Features.Tents.GetById;

public sealed record GetTentByIdQuery(
    Guid RetreatId,
    Guid TentId
) : IRequest<GetTentByIdResponse>;