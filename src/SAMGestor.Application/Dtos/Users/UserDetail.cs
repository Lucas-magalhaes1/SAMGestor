public sealed record UserDetail(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string Role,
    bool EmailConfirmed,
    bool Enabled,
    bool IsLocked,
    DateTimeOffset? LockoutEndAt,
    DateTimeOffset? LastLoginAt,
    string? PhotoUrl
    
);