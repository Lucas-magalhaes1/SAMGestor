public sealed record OutboxMessageDto(
    Guid Id,
    string Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    int Attempts,
    string? LastError
);