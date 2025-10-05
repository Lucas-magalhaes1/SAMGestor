using MediatR;

namespace SAMGestor.Application.Features.Service.Roster.Unassigned;

public sealed record GetUnassignedServiceMembersQuery(
    Guid   RetreatId,
    string? Gender = null,  // "Male" | "Female"
    string? City   = null,
    string? Search = null
) : IRequest<GetUnassignedServiceMembersResponse>;