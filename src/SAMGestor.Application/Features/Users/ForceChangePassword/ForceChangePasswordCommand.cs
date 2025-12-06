using MediatR;

namespace SAMGestor.Application.Features.Users.ForceChangePassword;

public sealed record ForceChangePasswordCommand(Guid UserId, string NewPassword) : IRequest<Unit>;