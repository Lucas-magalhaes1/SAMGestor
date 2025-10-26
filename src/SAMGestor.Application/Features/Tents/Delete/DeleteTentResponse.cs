namespace SAMGestor.Application.Features.Tents.Delete;

public sealed record DeleteTentResponse(
    Guid RetreatId,
    Guid TentId,
    int  Version   
);