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
    public int TotalSlots { get; private set; }
    public DateOnly RegistrationStart { get; private set; }
    public DateOnly RegistrationEnd { get; private set; }
    public Percentage WestRegionPercentage { get; private set; }
    public Percentage OtherRegionsPercentage { get; private set; }

    private Retreat() { }

    public Retreat(FullName name,
        string edition,
        string theme,
        DateOnly startDate,
        DateOnly endDate,
        int totalSlots,
        DateOnly registrationStart,
        DateOnly registrationEnd,
        Percentage westPct,
        Percentage othersPct)
    {
        if (endDate < startDate) throw new ArgumentException(nameof(endDate));
        if (registrationEnd < registrationStart) throw new ArgumentException(nameof(registrationEnd));

        Id = Guid.NewGuid();
        Name = name;
        Edition = edition.Trim();
        Theme = theme.Trim();
        StartDate = startDate;
        EndDate = endDate;
        TotalSlots = totalSlots;
        RegistrationStart = registrationStart;
        RegistrationEnd = registrationEnd;
        WestRegionPercentage = westPct;
        OtherRegionsPercentage = othersPct;
    }

    public bool RegistrationWindowOpen(DateOnly today) =>
        today >= RegistrationStart && today <= RegistrationEnd;
}