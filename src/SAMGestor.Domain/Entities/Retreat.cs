
using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Retreat : Entity<Guid>
{
    public FullName Name { get; private set; }
    public string Edition { get; private set; }
    public string Theme { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    public int MaleSlots { get; private set; }
    public int FemaleSlots { get; private set; }
    public int TotalSlots => MaleSlots + FemaleSlots;

    public DateOnly RegistrationStart { get; private set; }
    public DateOnly RegistrationEnd   { get; private set; }

    public Money FeeFazer  { get; private set; }
    public Money FeeServir { get; private set; }

    public Percentage WestRegionPercentage   { get; private set; }
    public Percentage OtherRegionsPercentage { get; private set; }

    public bool ContemplationClosed { get; private set; }

    private Retreat() { }

    public Retreat(FullName name,
                   string edition,
                   string theme,
                   DateOnly startDate,
                   DateOnly endDate,
                   int maleSlots,
                   int femaleSlots,
                   DateOnly registrationStart,
                   DateOnly registrationEnd,
                   Money feeFazer,
                   Money feeServir,
                   Percentage westPct,
                   Percentage othersPct)
    {
        if (endDate < startDate)               throw new ArgumentException(nameof(endDate));
        if (registrationEnd < registrationStart) throw new ArgumentException(nameof(registrationEnd));

        Id       = Guid.NewGuid();
        Name     = name;
        Edition  = edition.Trim();
        Theme    = theme.Trim();
        StartDate = startDate;
        EndDate   = endDate;

        MaleSlots   = maleSlots;
        FemaleSlots = femaleSlots;

        RegistrationStart = registrationStart;
        RegistrationEnd   = registrationEnd;

        FeeFazer  = feeFazer;
        FeeServir = feeServir;

        WestRegionPercentage   = westPct;
        OtherRegionsPercentage = othersPct;

        ContemplationClosed = false;
    }
    
    public void UpdateDetails(
        FullName   name,
        string     edition,
        string     theme,
        DateOnly   startDate,
        DateOnly   endDate,
        int        maleSlots,
        int        femaleSlots,
        DateOnly   registrationStart,
        DateOnly   registrationEnd,
        Money      feeFazer,
        Money      feeServir,
        Percentage westPct,
        Percentage othersPct)
    {
        if (endDate < startDate)                 throw new ArgumentException(nameof(endDate));
        if (registrationEnd < registrationStart) throw new ArgumentException(nameof(registrationEnd));

        Name     = name;
        Edition  = edition.Trim();
        Theme    = theme.Trim();
        StartDate = startDate;
        EndDate   = endDate;

        MaleSlots   = maleSlots;
        FemaleSlots = femaleSlots;

        RegistrationStart = registrationStart;
        RegistrationEnd   = registrationEnd;

        FeeFazer  = feeFazer;
        FeeServir = feeServir;

        WestRegionPercentage   = westPct;
        OtherRegionsPercentage = othersPct;
    }

    public void CloseContemplation() => ContemplationClosed = true;

    public bool RegistrationWindowOpen(DateOnly today) =>
        today >= RegistrationStart && today <= RegistrationEnd;
}
