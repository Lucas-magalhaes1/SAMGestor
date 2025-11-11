using MediatR;
using SAMGestor.Application.Dtos.Users;

namespace SAMGestor.Application.Features.Users.GetCredentials;

public sealed record GetUserCredentialsQuery(Guid Id) : IRequest<UserCredentialsResponse>;