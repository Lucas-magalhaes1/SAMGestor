namespace SAMGestor.Application.Dtos.Users;

public sealed record UserSummary(
    Guid Id,
    string Name,
    string Email,
    string Role
);