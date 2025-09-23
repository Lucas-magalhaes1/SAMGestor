using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.Create;

public sealed record CreateFamilyGroupsCommand(
    Guid RetreatId,
    bool ForceRecreate = false,
    bool DryRun        = false
) : IRequest<CreateFamilyGroupsResponse>;

public sealed record CreateFamilyGroupsResponse(
    int TotalFamilies,
    int Queued,
    int Skipped
);