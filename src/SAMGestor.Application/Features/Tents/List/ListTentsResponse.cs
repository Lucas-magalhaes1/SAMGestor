namespace SAMGestor.Application.Features.Tents.List;

public sealed record ListTentsResponse(
    IReadOnlyList<TentListItemDto> Items,
    int Total
);

public sealed record TentListItemDto(
    Guid TentId,
    string Number,
    string Category, 
    int Capacity,
    int AssignedCount,
    bool IsLocked,
    bool IsActive
);