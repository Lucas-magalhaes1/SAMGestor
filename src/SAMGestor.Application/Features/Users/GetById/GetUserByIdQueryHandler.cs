using MediatR;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Common;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.GetById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserSummary>
{
    private readonly IUserRepository _users;

    public GetUserByIdQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserSummary> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(request.Id, ct);
        if (u is null) throw new KeyNotFoundException("User not found");

        return new UserSummary(u.Id, u.Name.Value, u.Email.Value, u.Role.ToShortName());
    }
}