using MediatR;
using System;
using System.Collections.Generic;

namespace SAMGestor.Application.Features.Families.Update;

public sealed record UpdateFamiliesCommand(
    Guid RetreatId,
    int Version,
    IReadOnlyList<UpdateFamilyDto> Families,
    bool IgnoreWarnings = false
) : IRequest<UpdateFamiliesResponse>;

public sealed record UpdateFamilyDto(
    Guid FamilyId,
    string Name,
    int Capacity,
    IReadOnlyList<UpdateMemberDto> Members
);

public sealed record UpdateMemberDto(
    Guid RegistrationId,
    int Position
);