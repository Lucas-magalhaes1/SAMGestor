using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Entities;
public class Registration : Entity<Guid>
{
    public FullName Name { get; private set; } 
    public CPF Cpf { get; private set; }    
    public EmailAddress Email { get; private set; }
    public string Phone { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public Gender Gender { get; private set; }
    public string City { get; private set; }
    public UrlAddress? PhotoUrl { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public ParticipationCategory ParticipationCategory { get; private set; }
    public bool Enabled { get; private set; }
    public string Region { get; private set; }
    
    public Guid? TentId { get; private set; }
    public Guid RetreatId { get; private set; }
    public Guid? TeamId { get; private set; }
    public bool CompletedRetreat { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    
    private Registration() { }
    
    public Registration(FullName name,
        CPF cpf,
        EmailAddress email,
        string phone,
        DateOnly birthDate,
        Gender gender,
        string city,
        RegistrationStatus status,
        ParticipationCategory participationCategory,
        string region,
        Guid retreatId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Cpf = cpf;
        Email = email;
        Phone = phone.Trim();
        BirthDate = birthDate;
        Gender = gender;
        City = city.Trim();
        Status = status;
        ParticipationCategory = participationCategory;
        Enabled = true;
        Region = region.Trim();
        RetreatId = retreatId;
        CompletedRetreat = false;
        RegistrationDate = DateTime.UtcNow;
    }

    public void Disable() => Enabled = false;
    public void CompleteRetreat() => CompletedRetreat = true;
    
    public void SetStatus(RegistrationStatus newStatus)
    {
        Status = newStatus;
    }
    public void MarkConfirmed()
    {
        if (Status == RegistrationStatus.Canceled) return;
        Status = RegistrationStatus.Confirmed;
    }
    public bool IsEligibleForTent() =>
        Enabled && (Status == RegistrationStatus.PaymentConfirmed || Status == RegistrationStatus.Confirmed);

}