using MediatR;

namespace SAMGestor.Application.Features.Tents.TentRoster.Update;

public sealed record UpdateTentRosterCommand(
    Guid RetreatId,
    int  Version,
    List<UpdateTentSnapshot> Tents,
    bool IgnoreWarnings = false
) : IRequest<UpdateTentRosterResponse>;

public sealed record UpdateTentSnapshot(
    Guid TentId,
    List<UpdateTentMember> Members
);

public sealed record UpdateTentMember(
    Guid RegistrationId,
    int  Position
);

public sealed record UpdateTentRosterResponse(
    int Version,
    List<TentResult> Tents,
    List<RosterError> Errors,
    List<RosterWarning> Warnings
);

public sealed record TentResult(
    Guid   TentId,
    string Number,
    int    Capacity,
    int    AssignedCount,
    int    Remaining
);

public sealed record RosterError(
    string Code,
    string Message,
    Guid?  TentId,
    Guid[] RegistrationIds
);

public sealed record RosterWarning(
    string Code,
    string Message,
    Guid?  TentId
);