using MediatR;

namespace SAMGestor.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;