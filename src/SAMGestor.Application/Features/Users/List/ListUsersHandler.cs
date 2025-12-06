using MediatR;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Domain.Interfaces;

public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, ListUsersResponse>
{
    private readonly IUserRepository _users;

    public ListUsersHandler(IUserRepository users) => _users = users;

    public async Task<ListUsersResponse> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var (users, total) = await _users.ListAsync(request.Skip, request.Take, request.Search, ct);
        
        var items = users.Select(u => new UserListItem(
            u.Id,
            u.Name.Value,
            u.Email.Value,
            u.Role.ToShortName(),
            u.Enabled,
            u.EmailConfirmed
        )).ToList();

        return new ListUsersResponse(items, total);
    }
}