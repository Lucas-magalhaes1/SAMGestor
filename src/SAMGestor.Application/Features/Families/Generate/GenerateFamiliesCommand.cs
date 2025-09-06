using MediatR;
using System;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed record GenerateFamiliesCommand(
    Guid RetreatId,
    int? Capacity = null,
    bool ReplaceExisting = true
) : IRequest<GenerateFamiliesResponse>;