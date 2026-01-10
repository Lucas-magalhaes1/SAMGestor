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
    public bool Enabled { get; private set; }
    public Guid? TentId { get; private set; }
    public Guid RetreatId { get; private set; }
    public Guid? TeamId { get; private set; }
    public bool CompletedRetreat { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    public MaritalStatus? MaritalStatus { get; private set; }
    public PregnancyStatus Pregnancy { get; private set; } = PregnancyStatus.None;
    public ShirtSize? ShirtSize { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public string? Profession { get; private set; }
    public string? StreetAndNumber { get; private set; }
    public string? Neighborhood { get; private set; }
    public UF? State { get; private set; }
    public string? Whatsapp { get; private set; }
    public string? FacebookUsername { get; private set; }
    public string? InstagramHandle  { get; private set; }
    public string? NeighborPhone    { get; private set; }
    public string? RelativePhone    { get; private set; }
    public bool TermsAccepted { get; private set; }
    public DateTime? TermsAcceptedAt { get; private set; }
    public string? TermsVersion { get; private set; }
    public bool MarketingOptIn { get; private set; }
    public DateTime? MarketingOptInAt { get; private set; }
    public string? ClientIp { get; private set; }
    public string? UserAgent { get; private set; }
    public ParentStatus? FatherStatus { get; private set; } 
    public string? FatherName { get; private set; }
    public string? FatherPhone { get; private set; }
    public ParentStatus? MotherStatus { get; private set; }
    public string? MotherName { get; private set; }
    public string? MotherPhone { get; private set; }
    public bool? HadFamilyLossLast6Months { get; private set; } 
    public string? FamilyLossDetails { get; private set; }    
    public bool? HasRelativeOrFriendSubmitted { get; private set; } 
    public RelationshipDegree SubmitterRelationship { get; private set; } = RelationshipDegree.None; 
    public string? SubmitterNames { get; private set; }
    public string Religion { get; private set; } 
    public RahaminAttempt PreviousUncalledApplications { get; private set; } = RahaminAttempt.None;
    public AlcoholUsePattern AlcoholUse { get; private set; } = AlcoholUsePattern.None;
    public bool? Smoker { get; private set; }                   
    public bool? UsesDrugs { get; private set; }               
    public string? DrugUseFrequency { get; private set; }       
    public bool? HasAllergies { get; private set; }             
    public string? AllergiesDetails { get; private set; }       
    public bool? HasMedicalRestriction { get; private set; }    
    public string? MedicalRestrictionDetails { get; private set; } 
    public bool? TakesMedication { get; private set; }         
    public string? MedicationsDetails { get; private set; }     
    public string? PhysicalLimitationDetails { get; private set; }   
    public string? RecentSurgeryOrProcedureDetails { get; private set; } 
    public RahaminVidaEdition RahaminVidaCompleted { get; private set; } = RahaminVidaEdition.None;
    public string? PhotoStorageKey { get; private set; }   
    public string? PhotoContentType { get; private set; }  
    public int?    PhotoSizeBytes   { get; private set; }
    public DateTime? PhotoUploadedAt { get; private set; }
    public IdDocumentType? IdDocumentType { get; private set; }
    public string?         IdDocumentNumber { get; private set; }         
    public string?         IdDocumentStorageKey { get; private set; }
    public UrlAddress?     IdDocumentUrl { get; private set; }           
    public string?         IdDocumentContentType { get; private set; }    
    public int?            IdDocumentSizeBytes { get; private set; }
    public DateTime?       IdDocumentUploadedAt { get; private set; }

    private Registration() { }
    
    public Registration(FullName name,
        CPF cpf,
        EmailAddress email,
        string phone,
        DateOnly birthDate,
        Gender gender,
        string city,
        RegistrationStatus status,
        Guid retreatId)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Cpf = cpf;
        Email = email;
        Phone = phone.Trim();
        BirthDate = birthDate;
        Gender = gender;
        City = city.Trim();
        Status = status;
        Enabled = true;
        RetreatId = retreatId;
        CompletedRetreat = false;
        RegistrationDate = DateTime.UtcNow;

        TermsAccepted = false;
        MarketingOptIn = false;
    }
    
    public void SetMaritalStatus(MaritalStatus? status) => MaritalStatus = status;
    public void SetPregnancy(PregnancyStatus status) => Pregnancy = status;
    public void SetShirtSize(ShirtSize? size) => ShirtSize = size;
    public void SetAnthropometrics(decimal? weightKg, decimal? heightCm)
    { WeightKg = weightKg; HeightCm = heightCm; }
    public void SetProfession(string? profession)
    { Profession = string.IsNullOrWhiteSpace(profession) ? null : profession.Trim(); }
    public void SetAddress(string? streetAndNumber, string? neighborhood, UF? state, string? city = null)
    {
        StreetAndNumber = string.IsNullOrWhiteSpace(streetAndNumber) ? null : streetAndNumber.Trim();
        Neighborhood    = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim();
        State = state;
        if (!string.IsNullOrWhiteSpace(city)) City = city!.Trim();
    }
    public void SetWhatsapp(string? value)         => Whatsapp = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetFacebookUsername(string? value) => FacebookUsername = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetInstagramHandle(string? value)  => InstagramHandle  = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetNeighborPhone(string? value)    => NeighborPhone    = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetRelativePhone(string? value)    => RelativePhone    = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetFather(ParentStatus? status, string? name, string? phone)
    {
        FatherStatus = status;
        FatherName   = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        FatherPhone  = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
    public void SetMother(ParentStatus? status, string? name, string? phone)
    {
        MotherStatus = status;
        MotherName   = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        MotherPhone  = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
    public void SetFamilyLoss(bool? hadLoss, string? details)
    {
        HadFamilyLossLast6Months = hadLoss;
        FamilyLossDetails        = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    }
    public void SetSubmitterInfo(bool? hasSubmitter, RelationshipDegree relationshipFlags, string? names)
    {
        HasRelativeOrFriendSubmitted = hasSubmitter;
        SubmitterRelationship = relationshipFlags;
        SubmitterNames = string.IsNullOrWhiteSpace(names) ? null : names.Trim();
    }
    
    public void AcceptTerms(string versionOrHash, DateTime acceptedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(versionOrHash))
            throw new ArgumentException("Versão/identificador da política é obrigatório.", nameof(versionOrHash));
        TermsAccepted = true;
        TermsAcceptedAt = acceptedAtUtc;
        TermsVersion = versionOrHash.Trim();
    }
    public void SetMarketingOptIn(bool optIn, DateTime utcNow)
    { MarketingOptIn = optIn; MarketingOptInAt = optIn ? utcNow : null; }
    public void SetClientContext(string? clientIp, string? userAgent)
    {
        ClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    }
    
    public void SetReligion(string value) => Religion = value.Trim();
    public void SetPreviousUncalledApplications(RahaminAttempt attempts) => PreviousUncalledApplications = attempts;
    
    public void SetAlcoholUse(AlcoholUsePattern value) => AlcoholUse = value;

    public void SetSmoker(bool? value) => Smoker = value;

    public void SetDrugUse(bool? uses, string? frequency)
    {
        UsesDrugs = uses;
        DrugUseFrequency = string.IsNullOrWhiteSpace(frequency) ? null : frequency.Trim();
    }

    public void SetAllergies(bool? has, string? details)
    {
        HasAllergies = has;
        AllergiesDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    }

    public void SetMedicalRestriction(bool? has, string? details)
    {
        HasMedicalRestriction = has;
        MedicalRestrictionDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    }

    public void SetMedications(bool? takes, string? details)
    {
        TakesMedication = takes;
        MedicationsDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    }
    
    public void SetPhysicalLimitationDetails(string? details)
        => PhysicalLimitationDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();

    public void SetRecentSurgeryOrProcedureDetails(string? details)
        => RecentSurgeryOrProcedureDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
    
    public void SetRahaminVidaCompleted(RahaminVidaEdition editions)
        => RahaminVidaCompleted = editions;
    
    public void SetPhoto(string storageKey, string? contentType, int? sizeBytes, DateTime uploadedAt, UrlAddress? publicUrl = null)
    {
        PhotoStorageKey  = storageKey;
        PhotoContentType = contentType;
        PhotoSizeBytes   = sizeBytes;
        PhotoUploadedAt  = uploadedAt;
        if (publicUrl is not null) PhotoUrl = publicUrl; 
    }
    
    public void SetIdDocument(
        IdDocumentType type,
        string? number,
        string storageKey,
        string? contentType,
        int? sizeBytes,
        DateTime uploadedAt,
        UrlAddress? publicUrl = null)
    {
        IdDocumentType        = type;
        IdDocumentNumber      = string.IsNullOrWhiteSpace(number) ? null : number.Trim();
        IdDocumentStorageKey  = storageKey;
        IdDocumentContentType = contentType;
        IdDocumentSizeBytes   = sizeBytes;
        IdDocumentUploadedAt  = uploadedAt;
        if (publicUrl is not null) IdDocumentUrl = publicUrl; 
    }
    
    public void Disable() => Enabled = false;
    public void CompleteRetreat() => CompletedRetreat = true;
    public void SetStatus(RegistrationStatus newStatus) => Status = newStatus;
    public void MarkConfirmed()
    {
        if (Status == RegistrationStatus.Canceled) return;
        Status = RegistrationStatus.Confirmed;
    }
    public bool IsEligibleForTent() =>
        Enabled && (Status == RegistrationStatus.PaymentConfirmed || Status == RegistrationStatus.Confirmed);

    public int GetAgeOn(DateOnly onDate)
    {
        int age = onDate.Year - BirthDate.Year;
        if (new DateOnly(onDate.Year, BirthDate.Month, BirthDate.Day) > onDate) age--;
        return age;
    }

    public void ConfirmManualPayment()
    {
        if (Status == RegistrationStatus.Canceled)
            throw new InvalidOperationException("Não é possível confirmar pagamento de inscrição cancelada.");
    
        if (Status != RegistrationStatus.Selected)
            throw new InvalidOperationException("Apenas inscrições contempladas podem ter pagamento manual confirmado.");
    
        Status = RegistrationStatus.PaymentConfirmed;
    }

}
