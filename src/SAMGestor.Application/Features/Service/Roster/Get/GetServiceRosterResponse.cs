using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Roster.Get;

public sealed record GetServiceRosterResponse(
    int Version,
    IReadOnlyList<RosterSpaceView> Spaces
);

public sealed record RosterSpaceView(
    Guid SpaceId,
    string Name,
    string? Description,
    int MinPeople,
    int MaxPeople,
    bool IsLocked,
    bool IsActive,
    IReadOnlyList<RosterMemberView> Members
);

public sealed record RosterMemberView(
    Guid RegistrationId,
    string Name,
    ServiceRole Role,
    int Position,
    string? City
);