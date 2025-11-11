namespace SAMGestor.Application.Dtos.Users;

public sealed record UserCredentialsResponse(
    string Login,
    string Email,
    bool EmailVerified
);