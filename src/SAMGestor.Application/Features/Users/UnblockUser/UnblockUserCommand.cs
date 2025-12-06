using MediatR;

namespace SAMGestor.Application.Features.Users.UnblockUser;

public sealed record UnblockUserCommand(Guid UserId) : IRequest<Unit>;