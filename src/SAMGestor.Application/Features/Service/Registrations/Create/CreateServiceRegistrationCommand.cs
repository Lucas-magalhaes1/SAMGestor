using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Service.Registrations.Create;

public sealed record CreateServiceRegistrationCommand(
    Guid         RetreatId,
    FullName     Name,
    CPF          Cpf,
    EmailAddress Email,
    string       Phone,
    DateOnly     BirthDate,
    Gender       Gender,
    string       City,
    string       Region,
    Guid?        PreferredSpaceId
) : IRequest<CreateServiceRegistrationResponse>;