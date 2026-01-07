using MediatR;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.List;

public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, PagedResult<UserListItem>>
{
    private readonly IUserRepository _users;

    public ListUsersHandler(IUserRepository users) => _users = users;

    public async Task<PagedResult<UserListItem>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var result = await _users.ListAsync(request.Skip, request.Take, request.Search, ct);
        
        var items = result.Items.Select(u => new UserListItem(
            u.Id,
            u.Name.Value,
            u.Email.Value,
            u.Role.ToShortName(),
            u.Enabled,
            u.EmailConfirmed
        )).ToList();

        return new PagedResult<UserListItem>(items, result.TotalCount, request.Skip, request.Take);
    }
}