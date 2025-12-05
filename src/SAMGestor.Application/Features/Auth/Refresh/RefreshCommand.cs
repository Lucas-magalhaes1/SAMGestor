using MediatR;
using SAMGestor.Application.Dtos.Auth;

namespace SAMGestor.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(
    string AccessToken,
    string RefreshToken,
    string? UserAgent = null,
    string? IpAddress = null
) : IRequest<RefreshResponse>;