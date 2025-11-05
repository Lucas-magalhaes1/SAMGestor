namespace SAMGestor.Application.Features.Registrations.GetAll;

public record RegistrationDto(
    Guid     Id,
    string   Name,
    string   Cpf,
    string   Status,
    string   Gender,
    int      Age,
    string   City,
    string?  State,
    DateTime RegistrationDate,
    string?  PhotoUrl   
);