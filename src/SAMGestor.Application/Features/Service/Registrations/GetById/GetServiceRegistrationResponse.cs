using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed record GetServiceRegistrationResponse(
    Guid   Id,
    Guid   RetreatId,
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate,
    Gender Gender,
    string City,
    string Region,
    string? PhotoUrl,
    ServiceRegistrationStatus Status,
    bool   Enabled,
    DateTime RegistrationDateUtc,
    PreferredSpaceView? PreferredSpace
);

public sealed record PreferredSpaceView(
    Guid   Id,
    string Name
);