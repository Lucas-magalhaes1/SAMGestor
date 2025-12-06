using MediatR;

namespace SAMGestor.Application.Features.Users.ForceChangeEmail;

public sealed record ForceChangeEmailCommand(Guid UserId, string NewEmail) : IRequest<Unit>;