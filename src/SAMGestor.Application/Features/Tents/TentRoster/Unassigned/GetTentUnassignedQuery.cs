using MediatR;

namespace SAMGestor.Application.Features.Tents.TentRoster.Unassigned;

public sealed record GetTentUnassignedQuery(
    Guid RetreatId,
    string? Gender = null,   // "Male" | "Female" | null
    string? Search = null
) : IRequest<GetTentUnassignedResponse>;

public sealed record GetTentUnassignedResponse(
    Guid RetreatId,
    IReadOnlyList<UnassignedMemberView> Items
);

public sealed record UnassignedMemberView(
    Guid   RegistrationId,
    string Name,
    string Gender,
    string? City
);