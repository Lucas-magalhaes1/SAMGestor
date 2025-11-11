using MediatR;

namespace SAMGestor.Application.Features.Users.Delete;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Unit>;