using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Create;

public sealed class CreateServiceSpaceHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateServiceSpaceCommand, CreateServiceSpaceResponse>
{
    public async Task<CreateServiceSpaceResponse> Handle(CreateServiceSpaceCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (await spaceRepo.ExistsByNameInRetreatAsync(cmd.RetreatId, cmd.Name, ct))
            throw new BusinessRuleException("Já existe um espaço com esse nome neste retiro.");

        var space = new ServiceSpace(
            retreatId: cmd.RetreatId,
            name: cmd.Name.Trim(),
            description: string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description!.Trim(),
            maxPeople: cmd.MaxPeople,
            minPeople: cmd.MinPeople
        );

        if (!cmd.IsActive)
            space.Deactivate(); 

        await spaceRepo.AddAsync(space, ct);

        retreat.BumpServiceSpacesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);

        await uow.SaveChangesAsync(ct);

        return new CreateServiceSpaceResponse(space.Id, retreat.ServiceSpacesVersion);
    }
}