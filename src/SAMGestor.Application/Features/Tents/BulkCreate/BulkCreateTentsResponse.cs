using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.BulkCreate;

public sealed record BulkCreateTentsResponse(
    Guid RetreatId,
    int Created,   
    int Skipped,   
    IReadOnlyList<BulkCreatedTentView> Tents,    
    IReadOnlyList<BulkCreateTentError> Errors    
);

public sealed record BulkCreatedTentView(
    Guid TentId,
    string Number,
    TentCategory Category,
    int Capacity,
    bool IsActive,
    bool IsLocked,
    string? Notes
);

public sealed record BulkCreateTentError(
    string Code,            // e.g. DUPLICATE_PAYLOAD, DUPLICATE_EXISTING, INVALID_NUMBER, LOCKED
    string Message,
    string Number,
    TentCategory Category
);