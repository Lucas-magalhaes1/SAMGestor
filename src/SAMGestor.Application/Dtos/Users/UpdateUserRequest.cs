namespace SAMGestor.Application.Dtos.Users;

public sealed record UpdateUserRequest(
    Guid Id,
    string Name,
    string Phone
);