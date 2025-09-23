using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.Resend;

public sealed record ResendFamilyGroupCommand(
    Guid RetreatId,
    Guid FamilyId
) : IRequest<ResendFamilyGroupResponse>;

public sealed record ResendFamilyGroupResponse(bool Queued, string? Reason);

