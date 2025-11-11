using MediatR;

namespace SAMGestor.Application.Features.Users.Update;

public sealed record UpdateUserCommand(Guid Id, string Name, string Phone) : IRequest<Unit>;