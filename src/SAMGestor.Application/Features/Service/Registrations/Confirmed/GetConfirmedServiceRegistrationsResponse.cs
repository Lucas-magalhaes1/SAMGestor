using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Registrations.Confirmed;

public sealed record GetConfirmedServiceRegistrationsResponse(
    Guid   RegistrationId,
    string Name,
    string Email,
    string? Phone,
    Guid?  PreferredSpaceId,
    string? PreferredSpaceName,
    Guid?  AssignedSpaceId,
    string? AssignedSpaceName,
    ServiceRole? AssignedRole
);