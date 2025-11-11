using MediatR;

namespace SAMGestor.Application.Features.Auth.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email, string ResetUrlBase) : IRequest<Unit>;