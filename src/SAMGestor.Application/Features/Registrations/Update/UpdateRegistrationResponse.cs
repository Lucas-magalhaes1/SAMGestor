namespace SAMGestor.Application.Features.Registrations.Update;

public sealed record UpdateRegistrationResponse(
    Guid    RegistrationId,
    string? PhotoUrl,
    string? DocumentUrl
);