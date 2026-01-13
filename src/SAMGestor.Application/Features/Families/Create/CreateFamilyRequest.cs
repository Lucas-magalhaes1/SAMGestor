namespace SAMGestor.Application.Features.Families.Create;

public sealed record CreateFamilyRequest(
    string? Name,
    string ColorName,  
    int Capacity,
    IReadOnlyList<Guid> MemberIds,
    IReadOnlyList<Guid>? PadrinhoIds = null, 
    IReadOnlyList<Guid>? MadrinhaIds = null,
    bool IgnoreWarnings = false
);