using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.Create;

public sealed record CreateTentCommand(
    Guid RetreatId,
    string Number,            // virá como string; validamos e convertemos p/ int
    TentCategory Category,    // Male | Female
    int Capacity,             // > 0
    string? Notes = null
) : IRequest<CreateTentResponse>;