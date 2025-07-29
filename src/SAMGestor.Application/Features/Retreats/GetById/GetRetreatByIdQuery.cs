using MediatR;

namespace SAMGestor.Application.Features.Retreats.GetById;

public record GetRetreatByIdQuery(Guid Id) : IRequest<GetRetreatByIdResponse>;