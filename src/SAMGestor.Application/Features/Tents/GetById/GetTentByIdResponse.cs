using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.GetById;

public sealed record GetTentByIdResponse(
    Guid   TentId,
    Guid   RetreatId,
    string Number,
    TentCategory Category,
    int    Capacity,
    bool   IsActive,
    bool   IsLocked,
    string? Notes,
    int    AssignedCount
);