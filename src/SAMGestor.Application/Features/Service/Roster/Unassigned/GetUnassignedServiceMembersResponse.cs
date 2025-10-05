namespace SAMGestor.Application.Features.Service.Roster.Unassigned;

public sealed record GetUnassignedServiceMembersResponse(
    int Version,
    IReadOnlyList<UnassignedItem> Items
);

public sealed record UnassignedItem(
    Guid   RegistrationId,
    string Name,
    string? City,
    string Email,
    string Cpf,
    Guid?  PreferredSpaceId,
    string? PreferredSpaceName
);