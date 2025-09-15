using MediatR;

namespace SAMGestor.Application.Features.Families.Create;

public sealed record CreateFamilyCommand(
    Guid RetreatId,
    string? Name,
    IReadOnlyList<Guid> MemberIds,
    bool IgnoreWarnings = false
) : IRequest<CreateFamilyResult>;