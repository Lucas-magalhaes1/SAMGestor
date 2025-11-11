using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Dtos.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Phone,
    UserRole? Role 
);