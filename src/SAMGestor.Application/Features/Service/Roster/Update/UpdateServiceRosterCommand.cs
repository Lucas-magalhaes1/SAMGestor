using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Roster.Update;

public sealed record UpdateServiceRosterCommand(
    Guid RetreatId,
    int  Version,
    IReadOnlyList<SpaceInput> Spaces,
    bool IgnoreWarnings = false
) : IRequest<UpdateServiceRosterResponse>;

public sealed record SpaceInput(
    Guid SpaceId,
    string? Name,             
    IReadOnlyList<MemberInput> Members
);

public sealed record MemberInput(
    Guid RegistrationId,
    ServiceRole Role,
    int  Position
);