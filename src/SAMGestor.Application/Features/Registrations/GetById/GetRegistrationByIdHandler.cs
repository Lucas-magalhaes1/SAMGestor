using System.Globalization;
using MediatR;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Registrations.GetById;

public sealed class GetRegistrationByIdHandler(
    IRegistrationRepository regRepo,
    IFamilyMemberRepository familyMemberRepo,
    IFamilyRepository familyRepo,
    IStorageService storage,
    IManualPaymentProofRepository proofRepo 
) : IRequestHandler<GetRegistrationByIdQuery, GetRegistrationByIdResponse?>
{
    public async Task<GetRegistrationByIdResponse?> Handle(GetRegistrationByIdQuery q, CancellationToken ct)
    {
        var r = await regRepo.GetByIdAsync(q.RegistrationId, ct);
        if (r is null) return null;
        
        FamilyMembershipDto? familyDto = null;
        var link = await familyMemberRepo.GetByRegistrationIdAsync(r.RetreatId, r.Id, ct);
        if (link is not null)
        {
            var fam = await familyRepo.GetByIdAsync(link.FamilyId, ct);
            if (fam is not null)
            {
                familyDto = new FamilyMembershipDto(
                    fam.Id,
                    (string)fam.Name,
                    link.Position
                );
            }
        }

        var birthIso = r.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = r.GetAgeOn(today);

        var photoUrl = r.PhotoUrl?.Value;
        if (string.IsNullOrWhiteSpace(photoUrl) && !string.IsNullOrWhiteSpace(r.PhotoStorageKey))
            photoUrl = storage.GetPublicUrl(r.PhotoStorageKey);

        var docUrl = r.IdDocumentUrl?.Value;
        if (string.IsNullOrWhiteSpace(docUrl) && !string.IsNullOrWhiteSpace(r.IdDocumentStorageKey))
            docUrl = storage.GetPublicUrl(r.IdDocumentStorageKey);
        
        ManualPaymentProofDto? manualProofDto = null;
        var proof = await proofRepo.GetByRegistrationIdAsync(r.Id, ct);
        if (proof is not null)
        {
            manualProofDto = new ManualPaymentProofDto(
                ProofId: proof.Id,
                Amount: proof.Amount.Amount,
                Currency: proof.Amount.Currency,
                Method: proof.Method.ToString(),
                PaymentDate: proof.PaymentDate,
                UploadedAt: proof.ProofUploadedAt,
                Notes: proof.Notes,
                RegisteredBy: proof.RegisteredByUserId,
                RegisteredAt: proof.RegisteredAt
            );
        }

        return new GetRegistrationByIdResponse(
            r.Id,
            (string)r.Name,
            r.Cpf.Value,
            r.Email.Value,
            r.Phone,
            r.City,
            r.Gender.ToString(),
            r.Status.ToString(),
            r.Enabled,
            r.RetreatId,
            r.TentId,
            r.TeamId,
            birthIso,
            photoUrl,
            r.CompletedRetreat,
            r.RegistrationDate,
            familyDto,
            age,
            new PersonalDto(
                r.MaritalStatus?.ToString(),
                r.Pregnancy.ToString(),
                r.ShirtSize?.ToString(),
                r.WeightKg,
                r.HeightCm,
                r.Profession,
                r.StreetAndNumber,
                r.Neighborhood,
                r.State?.ToString()
            ),
            new ContactsDto(
                r.Whatsapp,
                r.FacebookUsername,
                r.InstagramHandle,
                r.NeighborPhone,
                r.RelativePhone
            ),
            new FamilyInfoDto(
                r.FatherStatus?.ToString(),
                r.FatherName,
                r.FatherPhone,
                r.MotherStatus?.ToString(),
                r.MotherName,
                r.MotherPhone,
                r.HadFamilyLossLast6Months,
                r.FamilyLossDetails,
                r.HasRelativeOrFriendSubmitted,
                r.SubmitterRelationship.ToString(),
                r.SubmitterNames
            ),
            new ReligionHistoryDto(
                r.Religion,
                r.PreviousUncalledApplications.ToString(),
                r.RahaminVidaCompleted.ToString()
            ),
            new HealthDto(
                r.AlcoholUse.ToString(),
                r.Smoker,
                r.UsesDrugs,
                r.DrugUseFrequency,
                r.HasAllergies,
                r.AllergiesDetails,
                r.HasMedicalRestriction,
                r.MedicalRestrictionDetails,
                r.TakesMedication,
                r.MedicationsDetails,
                r.PhysicalLimitationDetails,
                r.RecentSurgeryOrProcedureDetails
            ),
            new ConsentDto(
                r.TermsAccepted,
                r.TermsAcceptedAt,
                r.TermsVersion,
                r.MarketingOptIn,
                r.MarketingOptInAt,
                r.ClientIp,
                r.UserAgent
            ),
            new MediaDto(
                r.PhotoStorageKey,
                r.PhotoContentType,
                r.PhotoSizeBytes,
                r.PhotoUploadedAt,
                photoUrl,
                r.IdDocumentType?.ToString(),
                r.IdDocumentNumber,
                r.IdDocumentStorageKey,
                r.IdDocumentContentType,
                r.IdDocumentSizeBytes,
                r.IdDocumentUploadedAt,
                docUrl
            ),
            manualProofDto  
        );
    }
}
