using MediatR;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.List;

public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, PagedResult<UserListItem>>
{
    private readonly IUserRepository _users;
    private readonly IStorageService _storage; 

    public ListUsersHandler(IUserRepository users, IStorageService storage) 
    {
        _users = users;
        _storage = storage; 
    }

    public async Task<PagedResult<UserListItem>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var result = await _users.ListAsync(request.Skip, request.Take, request.Search, ct);
        
        var items = result.Items.Select(u =>
        {
            
            string? photoUrl = null;
            if (u.HasProfilePhoto() && !string.IsNullOrWhiteSpace(u.PhotoStorageKey))
            {
                photoUrl = _storage.GetPublicUrl(u.PhotoStorageKey);
            }

            return new UserListItem(
                u.Id,
                u.Name.Value,
                u.Email.Value,
                u.Role.ToShortName(),
                u.Enabled,
                u.EmailConfirmed,
                photoUrl 
            );
        }).ToList();

        return new PagedResult<UserListItem>(items, result.TotalCount, request.Skip, request.Take);
    }
}