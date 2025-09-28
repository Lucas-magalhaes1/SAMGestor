using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class ServiceRegistration : Entity<Guid>
{
    
    public FullName     Name        { get; private set; } = default!;
    public CPF          Cpf         { get; private set; }
    public EmailAddress Email       { get; private set; } = default!;
    public string       Phone       { get; private set; } = default!;
    public DateOnly     BirthDate   { get; private set; }
    public Gender       Gender      { get; private set; }
    public string       City        { get; private set; } = default!;
    public string       Region      { get; private set; } = default!;
    public UrlAddress?  PhotoUrl    { get; private set; }

    public Guid   RetreatId         { get; private set; }
    public Guid?  PreferredSpaceId  { get; private set; } 
    public ServiceRegistrationStatus Status { get; private set; }
    public bool   Enabled           { get; private set; }
    public DateTime RegistrationDate { get; private set; } 

    private ServiceRegistration() { }

    public ServiceRegistration(
        Guid retreatId,
        FullName name,
        CPF cpf,
        EmailAddress email,
        string phone,
        DateOnly birthDate,
        Gender gender,
        string city,
        string region,
        Guid? preferredSpaceId = null,
        UrlAddress? photoUrl   = null)
    {
        Id            = Guid.NewGuid();
        RetreatId     = retreatId;
        Name          = name;
        Cpf           = cpf;
        Email         = email;
        Phone         = phone.Trim();
        BirthDate     = birthDate;
        Gender        = gender;
        City          = city.Trim();
        Region        = region.Trim();
        PhotoUrl      = photoUrl;

        PreferredSpaceId = preferredSpaceId;
        Status           = ServiceRegistrationStatus.Submitted;
        Enabled          = true;
        RegistrationDate = DateTime.UtcNow;
    }

    public void MarkNotified()
    {
        if (Status != ServiceRegistrationStatus.Submitted && Status != ServiceRegistrationStatus.Notified)
            throw new InvalidOperationException("Invalid transition to Notified.");
        Status = ServiceRegistrationStatus.Notified;
    }

    public void Confirm()
    {
        if (Status is ServiceRegistrationStatus.Cancelled or ServiceRegistrationStatus.Declined)
            throw new InvalidOperationException("Cannot confirm a cancelled/declined registration.");
        Status = ServiceRegistrationStatus.Confirmed;
    }

    public void Decline()
    {
        if (Status == ServiceRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Already cancelled.");
        Status = ServiceRegistrationStatus.Declined;
    }

    public void Cancel()
    {
        if (Status == ServiceRegistrationStatus.Cancelled)
            return;
        Status = ServiceRegistrationStatus.Cancelled;
    }

    public void Disable() => Enabled = false;
    public void Enable()  => Enabled = true;

    public void UpdatePreferredSpace(Guid? preferredSpaceId)
    {
        PreferredSpaceId = preferredSpaceId;
    }
}
