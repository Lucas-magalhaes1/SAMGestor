namespace SAMGestor.Application.Features.Tents.Create;

public sealed record CreateTentResponse(
    Guid TentId,
    Guid RetreatId
);