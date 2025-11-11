using MediatR;
using SAMGestor.Application.Dtos.Users;

namespace SAMGestor.Application.Features.Users.Create;

public sealed record CreateUserCommand(string Name, string Email, string Phone, string? Role, string ConfirmUrlBase) : IRequest<CreateUserResponse>;