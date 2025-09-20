using MediatR;

namespace SAMGestor.Application.Features.Families.Groups;

public sealed record NotifyFamilyGroupCommand(
    Guid RetreatId,
    Guid FamilyId,
    string Channel,           // "email" | "whatsapp"
    bool ForceRecreate = false
) : IRequest<NotifyFamilyGroupResponse>;

public sealed record NotifyFamilyGroupResponse(
    bool Queued,
    bool Skipped,
    int  Version
);