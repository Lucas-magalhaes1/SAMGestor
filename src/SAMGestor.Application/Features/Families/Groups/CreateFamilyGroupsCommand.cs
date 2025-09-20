using MediatR;

namespace SAMGestor.Application.Features.Families.Groups;

public sealed record CreateFamilyGroupsCommand(
    Guid RetreatId,
    string Channel,            // "email" | "whatsapp"
    bool ForceRecreate = false,
    bool DryRun        = false
) : IRequest<CreateFamilyGroupsResponse>;

public sealed record CreateFamilyGroupsResponse(
    int TotalFamilies,
    int Queued,
    int Skipped,
    string Channel
);