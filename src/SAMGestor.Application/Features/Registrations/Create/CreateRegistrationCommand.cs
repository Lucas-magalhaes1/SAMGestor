using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Registrations.Create;

public record CreateRegistrationCommand(
    Guid         RetreatId,
    FullName     Name,
    CPF          Cpf,
    EmailAddress Email,
    string       Phone,
    DateOnly     BirthDate,
    Gender       Gender,
    string       City,
    ParticipationCategory ParticipationCategory,
    string       Region
) : IRequest<CreateRegistrationResponse>;