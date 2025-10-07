using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Roster.Update;

public sealed record UpdateServiceRosterResponse(
    int Version,
    IReadOnlyList<SpaceResult> Spaces,
    IReadOnlyList<RosterError> Errors,
    IReadOnlyList<RosterWarning> Warnings
);

public sealed record SpaceResult(
    Guid SpaceId,
    string Name,
    int MinPeople,
    int MaxPeople,
    int Count,
    bool HasCoordinator,
    bool HasVice
);

public sealed record RosterError(
    string Code,           // e.g. VERSION_MISMATCH, SPACE_LOCKED, UNKNOWN_SPACE, UNKNOWN_REGISTRATION, WRONG_RETREAT, DUPLICATE_LEADER, DUPLICATE_REGISTRATION
    string Message,
    Guid? SpaceId,
    IReadOnlyList<Guid> RegistrationIds
);

public sealed record RosterWarning(
    string Code,           // e.g. BelowMin, OverMax, MissingCoordinator, MissingVice
    string Message,
    Guid SpaceId
);