namespace SAMGestor.Application.Features.Retreats.GetById;


public record GetRetreatByIdResponse(
    Guid     Id,
    string   Name,
    string   Edition,
    string   Theme,
    DateOnly StartDate,
    DateOnly EndDate,
    int      MaleSlots,
    int      FemaleSlots,
    DateOnly RegistrationStart,
    DateOnly RegistrationEnd,
    decimal  FeeFazer,
    decimal  FeeServir,
    decimal  WestRegionPct,
    decimal  OtherRegionPct);