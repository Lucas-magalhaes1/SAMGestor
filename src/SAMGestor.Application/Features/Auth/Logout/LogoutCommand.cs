using MediatR;

namespace SAMGestor.Application.Features.Auth.Logout;

public sealed record LogoutCommand(
    string RefreshToken,
    Guid UserId
) : IRequest<Unit>;