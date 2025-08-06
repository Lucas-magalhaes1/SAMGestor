namespace SAMGestor.Application.Features.Registrations.GetAll;

public record GetAllRegistrationsResponse(
    IReadOnlyList<RegistrationDto> Items,
    int TotalCount,
    int Skip,
    int Take);