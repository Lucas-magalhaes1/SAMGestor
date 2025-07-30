using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Update;

public sealed class UpdateRetreatHandler(IRetreatRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateRetreatCommand, UpdateRetreatResponse>
{
    public async Task<UpdateRetreatResponse> Handle(
        UpdateRetreatCommand cmd,
        CancellationToken    ct)
    {
        var retreat = await repo.GetByIdAsync(cmd.Id, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.Id);
        
        if ((string)retreat.Name != (string)cmd.Name || retreat.Edition != cmd.Edition)
        {
            var duplicated = await repo.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, ct);
            if (duplicated)
                throw new BusinessRuleException("Retreat with same name and edition already exists.");
        }

        retreat.UpdateDetails(
            cmd.Name,
            cmd.Edition,
            cmd.Theme,
            cmd.StartDate,
            cmd.EndDate,
            cmd.MaleSlots,
            cmd.FemaleSlots,
            cmd.RegistrationStart,
            cmd.RegistrationEnd,
            cmd.FeeFazer,
            cmd.FeeServir,
            cmd.WestRegionPct,
            cmd.OtherRegionPct);

        await uow.SaveChangesAsync(ct);

        return new UpdateRetreatResponse(retreat.Id);
    }
}