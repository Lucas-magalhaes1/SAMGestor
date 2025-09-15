namespace SAMGestor.Application.Features.Registrations.GetById;

public sealed record GetRegistrationByIdResponse(
    Guid Id,
    string Name,
    string Cpf,
    string Email,
    string Phone,
    string City,
    string Gender,
    string Status,
    string ParticipationCategory,
    bool Enabled,
    string Region,
    Guid RetreatId,
    Guid? TentId,
    Guid? TeamId,
    string BirthDate,          
    string? PhotoUrl,          
    bool CompletedRetreat,
    DateTime RegistrationDate,
    FamilyMembershipDto? Family
);

public sealed record FamilyMembershipDto(
    Guid FamilyId,
    string FamilyName,
    int Position
);