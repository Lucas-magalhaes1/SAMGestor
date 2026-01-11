using MediatR;
using SAMGestor.Application.Common.Pagination;

namespace SAMGestor.Application.Features.Users.List;

public sealed record ListUsersQuery(
    string? Search = null,
    int Skip = 0,
    int Take = 20
) : IRequest<PagedResult<UserListItem>>;

public sealed record UserListItem(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool Enabled,
    bool EmailConfirmed,
    string? PhotoUrl
);