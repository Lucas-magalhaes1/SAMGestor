namespace SAMGestor.Application.Features.Registrations.GetAll;

public record RegistrationDto(
    Guid     Id,
    string   Name,
    string   Cpf,
    string   Status,
    string   Region,
    string   Category,
    DateTime RegistrationDate);