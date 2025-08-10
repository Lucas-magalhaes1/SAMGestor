using MediatR;

namespace SAMGestor.Application.Features.Lottery;

public sealed record ManualSelectCommand(Guid RetreatId, Guid RegistrationId) : IRequest<Unit>;
public sealed record ManualUnselectCommand(Guid RetreatId, Guid RegistrationId) : IRequest<Unit>;