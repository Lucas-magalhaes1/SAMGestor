using MediatR;
using SAMGestor.Application.Dtos.Auth;

namespace SAMGestor.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password, string? UserAgent, string? Ip) : IRequest<LoginResponse>;