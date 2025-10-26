using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.List;

public sealed record TentListItem(
    Guid   TentId,
    string Number,
    TentCategory Category,
    int    Capacity,
    bool   IsActive,
    bool   IsLocked,
    string? Notes,
    int    AssignedCount
);