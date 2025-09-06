using MediatR;
using System;

namespace SAMGestor.Application.Features.Families.GetById;

public sealed record GetFamilyByIdQuery(Guid RetreatId, Guid FamilyId, bool IncludeAlerts = true)
    : IRequest<GetFamilyByIdResponse>;