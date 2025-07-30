using MediatR;

namespace SAMGestor.Application.Features.Retreats.Delete;

public record DeleteRetreatCommand(Guid Id) : IRequest<DeleteRetreatResponse>;