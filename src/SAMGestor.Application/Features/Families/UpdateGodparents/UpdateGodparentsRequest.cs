namespace SAMGestor.Application.Features.Families.UpdateGodparents;

public sealed record UpdateGodparentsRequest(
    IReadOnlyList<Guid> PadrinhoIds,
    IReadOnlyList<Guid> MadrinhaIds
);