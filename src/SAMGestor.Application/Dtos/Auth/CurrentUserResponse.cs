namespace SAMGestor.Application.Dtos.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool EmailConfirmed
);