namespace SAMGestor.Application.Features.Registrations.GetById;

public sealed record GetRegistrationByIdResponse(
    Guid     Id,
    string   Name,
    string   Cpf,
    string   Email,
    string   Phone,
    string   City,
    string   Gender,
    string   Status,
    bool     Enabled,
    Guid     RetreatId,
    Guid?    TentId,
    Guid?    TeamId,
    string   BirthDate,
    string?  PhotoUrl,
    bool     CompletedRetreat,
    DateTime RegistrationDate,
    FamilyMembershipDto? Family,
    int      Age,
    
    PersonalDto        Personal,
    ContactsDto        Contacts,
    FamilyInfoDto      FamilyInfo,
    ReligionHistoryDto ReligionHistory,
    HealthDto          Health,
    ConsentDto         Consents,
    MediaDto           Media
);

public sealed record FamilyMembershipDto(
    Guid FamilyId,
    string FamilyName,
    int Position
);

public sealed record PersonalDto(
    string? MaritalStatus,
    string  Pregnancy,
    string? ShirtSize,
    decimal? WeightKg,
    decimal? HeightCm,
    string? Profession,
    string? StreetAndNumber,
    string? Neighborhood,
    string? State
);

public sealed record ContactsDto(
    string? Whatsapp,
    string? FacebookUsername,
    string? InstagramHandle,
    string? NeighborPhone,
    string? RelativePhone
);

public sealed record FamilyInfoDto(
    string? FatherStatus,
    string? FatherName,
    string? FatherPhone,
    string? MotherStatus,
    string? MotherName,
    string? MotherPhone,
    bool?   HadFamilyLossLast6Months,
    string? FamilyLossDetails,
    bool?   HasRelativeOrFriendSubmitted,
    string? SubmitterRelationship,
    string? SubmitterNames
);

public sealed record ReligionHistoryDto(
    string  Religion,
    string  PreviousUncalledApplications,
    string  RahaminVidaCompleted
);

public sealed record HealthDto(
    string  AlcoholUse,
    bool?   Smoker,
    bool?   UsesDrugs,
    string? DrugUseFrequency,
    bool?   HasAllergies,
    string? AllergiesDetails,
    bool?   HasMedicalRestriction,
    string? MedicalRestrictionDetails,
    bool?   TakesMedication,
    string? MedicationsDetails,
    string? PhysicalLimitationDetails,
    string? RecentSurgeryOrProcedureDetails
);

public sealed record ConsentDto(
    bool     TermsAccepted,
    DateTime? TermsAcceptedAt,
    string?  TermsVersion,
    bool     MarketingOptIn,
    DateTime? MarketingOptInAt,
    string?  ClientIp,
    string?  UserAgent
);

public sealed record MediaDto(
    string?   PhotoStorageKey,
    string?   PhotoContentType,
    int?      PhotoSizeBytes,
    DateTime? PhotoUploadedAt,
    string?   PhotoUrl,

    string?   IdDocumentType,
    string?   IdDocumentNumber,
    string?   IdDocumentStorageKey,
    string?   IdDocumentContentType,
    int?      IdDocumentSizeBytes,
    DateTime? IdDocumentUploadedAt,
    string?   IdDocumentUrl
);
