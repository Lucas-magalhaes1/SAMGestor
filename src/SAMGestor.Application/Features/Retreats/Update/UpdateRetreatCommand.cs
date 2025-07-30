using MediatR;
using SAMGestor.Application.Common.Retreat;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Retreats.Update;

public record UpdateRetreatCommand(
    Guid       Id,
    FullName   Name,
    string     Edition,
    string     Theme,
    DateOnly   StartDate,
    DateOnly   EndDate,
    int        MaleSlots,
    int        FemaleSlots,
    DateOnly   RegistrationStart,
    DateOnly   RegistrationEnd,
    Money      FeeFazer,
    Money      FeeServir,
    Percentage WestRegionPct,
    Percentage OtherRegionPct
) : IRequest<UpdateRetreatResponse>, IRetreatCommand;