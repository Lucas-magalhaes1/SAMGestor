using MediatR;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed record GenerateFamiliesCommand(
    Guid RetreatId,
    int MembersPerFamily,                   
    bool ReplaceExisting = true,
    bool FillExistingFirst = false 
) : IRequest<GenerateFamiliesResponse>;