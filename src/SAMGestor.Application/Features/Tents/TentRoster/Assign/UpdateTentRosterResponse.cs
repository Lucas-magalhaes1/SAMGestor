namespace SAMGestor.Application.Features.Tents.TentRoster.Assign;

public sealed record UpdateTentRosterResponse(
    int Version,
    IReadOnlyList<TentRosterSpaceView> Tents,
    IReadOnlyList<TentRosterError> Errors
);

public sealed record TentRosterSpaceView(
    Guid TentId,
    string Number,
    string Category,   // "Male"/"Female"
    int Capacity,
    bool IsLocked,
    IReadOnlyList<TentRosterMemberView> Members
);

public sealed record TentRosterMemberView(
    Guid RegistrationId,
    string Name,
    string Gender,      // "Male"/"Female"
    string? City,
    int? Position
);

public sealed record TentRosterError(
    string Code,        // e.g. VERSION_MISMATCH, TENT_LOCKED, OVER_CAPACITY, WRONG_CATEGORY, INVALID_MEMBER, DUPLICATED_MEMBER
    string Message,
    Guid? TentId,
    IReadOnlyList<Guid> RegistrationIds
);