using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Registrations.Create;

public sealed record CreateRegistrationCommand(
   
    Guid         RetreatId,
    FullName     Name,
    CPF          Cpf,
    EmailAddress Email,
    string       Phone,
    DateOnly     BirthDate,
    Gender       Gender,
    string       City,
    
    MaritalStatus   MaritalStatus,
    PregnancyStatus Pregnancy,
    ShirtSize       ShirtSize,
    decimal         WeightKg,
    decimal         HeightCm,
    string          Profession,
    string          StreetAndNumber,
    string          Neighborhood,
    UF              State,
    
    string? Whatsapp,
    string? FacebookUsername,
    string? InstagramHandle,
    string  NeighborPhone,
    string  RelativePhone,

    ParentStatus FatherStatus,
    string?      FatherName,
    string?      FatherPhone,
    ParentStatus MotherStatus,
    string?      MotherName,
    string?      MotherPhone,
    bool         HadFamilyLossLast6Months,
    string?      FamilyLossDetails,
    bool         HasRelativeOrFriendSubmitted,
    RelationshipDegree SubmitterRelationship,
    string?         SubmitterNames,
    
    string               Religion,
    RahaminAttempt       PreviousUncalledApplications,
    RahaminVidaEdition   RahaminVidaCompleted,
    
    AlcoholUsePattern AlcoholUse,
    bool             Smoker,
    bool             UsesDrugs,
    string?          DrugUseFrequency,
    bool             HasAllergies,
    string?          AllergiesDetails,
    bool             HasMedicalRestriction,
    string?          MedicalRestrictionDetails,
    bool             TakesMedication,
    string?          MedicationsDetails,
    string?          PhysicalLimitationDetails,
    string?          RecentSurgeryOrProcedureDetails,
    
    bool    TermsAccepted,
    string  TermsVersion,
    bool?   MarketingOptIn,
    string? ClientIp,
    string? UserAgent
) : IRequest<CreateRegistrationResponse>;
