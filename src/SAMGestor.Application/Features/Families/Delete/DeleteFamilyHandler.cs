using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Delete;

public sealed class DeleteFamilyHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IUnitOfWork uow
) : IRequestHandler<DeleteFamilyCommand, DeleteFamilyResponse>
{
    public async Task<DeleteFamilyResponse> Handle(DeleteFamilyCommand cmd, CancellationToken ct)
    {
       
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");
        
        var family = await familyRepo.GetByIdAsync(cmd.FamilyId, ct)
                     ?? throw new NotFoundException(nameof(Family), cmd.FamilyId);

        if (family.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Família não pertence ao retiro informado.");
        
        if (family.IsLocked)
            throw new BusinessRuleException($"Família '{family.Name}' está bloqueada e não pode ser removida.");
        
        if (family.GroupStatus == GroupStatus.Creating || family.GroupStatus == GroupStatus.Active)
        {
            throw new BusinessRuleException(
                $"Família '{family.Name}' possui grupo WhatsApp/Email ativo ou em criação. " +
                "Não é permitido deletar."
            );
        }

       
        var members = await familyMemberRepo.ListByFamilyAsync(family.Id, ct);
        var membersCount = members.Count;
        var familyName = (string)family.Name;

       
        await familyRepo.DeleteAsync(family, ct);

      
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new DeleteFamilyResponse(
            Version: retreat.FamiliesVersion,
            FamilyName: familyName,
            MembersDeleted: membersCount
        );
    }
}
