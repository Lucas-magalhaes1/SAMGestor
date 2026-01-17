// Application/Features/Registrations/Update/UpdateRegistrationHandler.cs
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Registrations.Update;

public sealed class UpdateRegistrationHandler(
    IRegistrationRepository regRepo,
    IUnitOfWork             uow,
    IStorageService         storage
    
) : IRequestHandler<UpdateRegistrationCommand, UpdateRegistrationResponse>
{
    public async Task<UpdateRegistrationResponse> Handle(
        UpdateRegistrationCommand cmd,
        CancellationToken ct)
    {
        
        var reg = await regRepo.GetByIdForUpdateAsync(cmd.RegistrationId, ct);
        if (reg is null)
            throw new NotFoundException(nameof(Registration), cmd.RegistrationId);
        
        if (reg.Cpf != cmd.Cpf)
        {
            if (await regRepo.IsCpfBlockedAsync(cmd.Cpf, ct))
                throw new BusinessRuleException("CPF is blocked.");

            if (await regRepo.ExistsByCpfInRetreatAsync(cmd.Cpf, reg.RetreatId, ct))
                throw new BusinessRuleException("CPF already registered for this retreat.");
        }

        reg.UpdateBasicInfo(cmd.Name, cmd.Cpf, cmd.Email, cmd.Phone, cmd.BirthDate, cmd.Gender, cmd.City);
      
        reg.SetMaritalStatus(cmd.MaritalStatus);
        reg.SetPregnancy(cmd.Pregnancy);
        reg.SetShirtSize(cmd.ShirtSize);
        reg.SetAnthropometrics(cmd.WeightKg, cmd.HeightCm);
        reg.SetProfession(cmd.Profession);
        reg.SetAddress(cmd.StreetAndNumber, cmd.Neighborhood, cmd.State, cmd.City);
        
        reg.SetWhatsapp(cmd.Whatsapp);
        reg.SetFacebookUsername(cmd.FacebookUsername);
        reg.SetInstagramHandle(cmd.InstagramHandle);
        reg.SetNeighborPhone(cmd.NeighborPhone);
        reg.SetRelativePhone(cmd.RelativePhone);

        reg.SetFather(cmd.FatherStatus, cmd.FatherName, cmd.FatherPhone);
        reg.SetMother(cmd.MotherStatus, cmd.MotherName, cmd.MotherPhone);
        reg.SetFamilyLoss(cmd.HadFamilyLossLast6Months, cmd.FamilyLossDetails);
        reg.SetSubmitterInfo(cmd.HasRelativeOrFriendSubmitted, cmd.SubmitterRelationship, cmd.SubmitterNames);

        reg.SetReligion(cmd.Religion);
        reg.SetPreviousUncalledApplications(cmd.PreviousUncalledApplications);
        reg.SetRahaminVidaCompleted(cmd.RahaminVidaCompleted);

        reg.SetAlcoholUse(cmd.AlcoholUse);
        reg.SetSmoker(cmd.Smoker);
        reg.SetDrugUse(cmd.UsesDrugs, cmd.DrugUseFrequency);
        reg.SetAllergies(cmd.HasAllergies, cmd.AllergiesDetails);
        reg.SetMedicalRestriction(cmd.HasMedicalRestriction, cmd.MedicalRestrictionDetails);
        reg.SetMedications(cmd.TakesMedication, cmd.MedicationsDetails);
        reg.SetPhysicalLimitationDetails(cmd.PhysicalLimitationDetails);
        reg.SetRecentSurgeryOrProcedureDetails(cmd.RecentSurgeryOrProcedureDetails);

        if (!string.IsNullOrWhiteSpace(cmd.PhotoStorageKey))
        {
            if (!string.IsNullOrWhiteSpace(reg.PhotoStorageKey))
            {
                await storage.DeleteAsync(reg.PhotoStorageKey, ct);
            }
        
            var publicUrl = new UrlAddress(cmd.PhotoUrl!);
            reg.SetPhoto(cmd.PhotoStorageKey, cmd.PhotoContentType!, (int)cmd.PhotoSize!.Value, DateTime.UtcNow, publicUrl);
        }

        if (!string.IsNullOrWhiteSpace(cmd.DocumentStorageKey) && cmd.DocumentType.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(reg.IdDocumentStorageKey))
            {
                 await storage.DeleteAsync(reg.IdDocumentStorageKey, ct);
            }
        
            var publicUrl = new UrlAddress(cmd.DocumentUrl!);
            reg.SetIdDocument(cmd.DocumentType.Value, cmd.DocumentNumber, cmd.DocumentStorageKey, 
                cmd.DocumentContentType!, (int)cmd.DocumentSize!.Value, DateTime.UtcNow, publicUrl);
        }

        await regRepo.UpdateAsync(reg, ct);
        await uow.SaveChangesAsync(ct);

        return new UpdateRegistrationResponse(reg.Id, cmd.PhotoUrl, cmd.DocumentUrl);
    }
}
