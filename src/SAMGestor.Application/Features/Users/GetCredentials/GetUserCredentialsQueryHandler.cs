using MediatR;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.GetCredentials;

public sealed class GetUserCredentialsQueryHandler : IRequestHandler<GetUserCredentialsQuery, UserCredentialsResponse>
{
    private readonly IUserRepository _users;

    public GetUserCredentialsQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserCredentialsResponse> Handle(GetUserCredentialsQuery request, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(request.Id, ct);
        if (u is null) throw new KeyNotFoundException("User not found");

        return new UserCredentialsResponse($"{u.Name.Value} login", u.Email.Value, u.EmailConfirmed);
    }
}