using MediatR;

public sealed record ListUsersQuery(
    int Skip = 0,
    int Take = 20,
    string? Search = null
) : IRequest<ListUsersResponse>;

public sealed record ListUsersResponse(
    List<UserListItem> Users,
    int Total
);

public sealed record UserListItem(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool Enabled,
    bool EmailConfirmed
);