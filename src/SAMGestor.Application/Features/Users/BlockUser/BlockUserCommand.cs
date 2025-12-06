using MediatR;

namespace SAMGestor.Application.Features.Users.BlockUser;

public sealed record BlockUserCommand(Guid UserId) : IRequest<Unit>;