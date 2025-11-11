using MediatR;
using SAMGestor.Application.Dtos.Users;

namespace SAMGestor.Application.Features.Users.GetById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserSummary>;