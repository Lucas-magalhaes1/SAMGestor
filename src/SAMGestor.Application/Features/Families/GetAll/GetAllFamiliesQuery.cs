using MediatR;
using System;

namespace SAMGestor.Application.Features.Families.GetAll;

public sealed record GetAllFamiliesQuery(Guid RetreatId, bool IncludeAlerts = true)
    : IRequest<GetAllFamiliesResponse>;