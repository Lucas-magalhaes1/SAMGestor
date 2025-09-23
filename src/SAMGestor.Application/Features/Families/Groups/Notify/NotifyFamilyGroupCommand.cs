using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.Notify;

public sealed record NotifyFamilyGroupCommand(
    Guid RetreatId,
    Guid FamilyId,
    bool ForceRecreate = false
) : IRequest<NotifyFamilyGroupResponse>;

public sealed record NotifyFamilyGroupResponse(
    bool Queued,
    bool Skipped,
    int  Version
);