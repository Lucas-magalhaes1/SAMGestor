using MediatR;

namespace SAMGestor.Application.Features.Families.Unassigned;

public sealed record GetUnassignedQuery(
    Guid RetreatId,
    string? Gender = null,
    string? City = null,
    string? Search = null
) : IRequest<GetUnassignedResponse>;

public sealed record GetUnassignedResponse(IReadOnlyList<UnassignedMemberDto> Items);

public sealed record UnassignedMemberDto(
    Guid RegistrationId,
    string Name,
    string Gender,
    string City,
    string Email
);