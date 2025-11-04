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
    public int FamiliesVersion { get; private set; } = 0;
    public bool FamiliesLocked { get; private set; }
    public bool ContemplationClosed { get; private set; }
    public int  ServiceSpacesVersion { get; private set; } = 0;
    public bool ServiceLocked        { get; private set; } = false;
    public int  TentsVersion { get; private set; } = 0;
    public bool TentsLocked  { get; private set; } = false;
    public string? PrivacyPolicyTitle { get; private set; }
    public string? PrivacyPolicyBody  { get; private set; }  
    public string? PrivacyPolicyVersion { get; private set; } 

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
        if (endDate < startDate)                 throw new ArgumentException(nameof(endDate));
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
        FamiliesVersion     = 0;
        FamiliesLocked = false;
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
    
    public void SetPrivacyPolicy(string title, string body, string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Versão/identificador da política é obrigatório.", nameof(version));

        PrivacyPolicyTitle   = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        PrivacyPolicyBody    = string.IsNullOrWhiteSpace(body)  ? null : body;
        PrivacyPolicyVersion = version.Trim();
    }

    public void CloseContemplation() => ContemplationClosed = true;

    public bool RegistrationWindowOpen(DateOnly today) =>
        today >= RegistrationStart && today <= RegistrationEnd;

    public void BumpFamiliesVersion() => FamiliesVersion++;
    public void LockFamilies() { FamiliesLocked = true; BumpFamiliesVersion(); }
    public void UnlockFamilies() { FamiliesLocked = false; BumpFamiliesVersion(); }
    public void BumpServiceSpacesVersion() => ServiceSpacesVersion++;
    public void LockService()  => ServiceLocked = true;
    public void UnlockService()=> ServiceLocked = false;
    public void BumpTentsVersion() => TentsVersion++;
    public void LockTents()   { TentsLocked = true;  BumpTentsVersion(); }
    public void UnlockTents() { TentsLocked = false; BumpTentsVersion(); }
}
