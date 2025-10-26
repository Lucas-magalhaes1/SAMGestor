using MediatR;

namespace SAMGestor.Application.Features.Tents.TentRoster.AutoAssign;

public sealed record AutoAssignTentsCommand(
    Guid RetreatId,
    bool RespectLocked = true   // default: não mexe em barracas travadas
) : IRequest<AutoAssignTentsResponse>;