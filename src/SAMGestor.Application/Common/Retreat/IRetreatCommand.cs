using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Common.Retreat;

public interface IRetreatCommand
{
    FullName   Name            { get; }
    string     Edition         { get; }
    string     Theme           { get; }
    DateOnly   StartDate       { get; }
    DateOnly   EndDate         { get; }
    int        MaleSlots       { get; }
    int        FemaleSlots     { get; }
    DateOnly   RegistrationStart { get; }
    DateOnly   RegistrationEnd   { get; }
    Money      FeeFazer        { get; }
    Money      FeeServir       { get; }
    Percentage WestRegionPct   { get; }
    Percentage OtherRegionPct  { get; }
}