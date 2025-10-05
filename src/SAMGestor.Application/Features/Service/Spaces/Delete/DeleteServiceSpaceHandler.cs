using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Delete;

public sealed class DeleteServiceSpaceHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignmentRepo,
    IServiceRegistrationRepository regRepo,
    IUnitOfWork uow
) : IRequestHandler<DeleteServiceSpaceCommand, DeleteServiceSpaceResponse>
{
    public async Task<DeleteServiceSpaceResponse> Handle(DeleteServiceSpaceCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var space = await spaceRepo.GetByIdAsync(cmd.SpaceId, ct)
                    ?? throw new NotFoundException(nameof(ServiceSpace), cmd.SpaceId);

        if (space.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Space não pertence a este retiro.");

        if (space.IsLocked)
            throw new BusinessRuleException("Espaço bloqueado não pode ser removido.");
        
        await assignmentRepo.RemoveBySpaceIdAsync(space.Id, ct);
        
        await regRepo.ClearPreferenceBySpaceIdAsync(space.Id, ct);
        
        await spaceRepo.RemoveAsync(space, ct);
        
        retreat.BumpServiceSpacesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new DeleteServiceSpaceResponse(true, retreat.ServiceSpacesVersion);
    }
}