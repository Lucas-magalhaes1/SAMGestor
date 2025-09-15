using MediatR;

namespace SAMGestor.Application.Features.Families.Delete;

public sealed record DeleteFamilyCommand(Guid RetreatId, Guid FamilyId) : IRequest<Unit>;