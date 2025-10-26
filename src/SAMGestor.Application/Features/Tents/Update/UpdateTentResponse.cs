namespace SAMGestor.Application.Features.Tents.Update;

public sealed record UpdateTentResponse(
    Guid TentId,
    Guid RetreatId,
    int  Version   
);