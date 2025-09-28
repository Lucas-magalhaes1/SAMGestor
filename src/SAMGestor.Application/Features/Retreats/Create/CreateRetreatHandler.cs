using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Services; 
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Create;

public sealed class CreateRetreatHandler
    : IRequestHandler<CreateRetreatCommand, CreateRetreatResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork        _uow;
    private readonly ServiceSpacesSeeder _spacesSeeder; 
    
    public CreateRetreatHandler(IRetreatRepository repo, IUnitOfWork uow, ServiceSpacesSeeder spacesSeeder)
    {
        _repo = repo;
        _uow  = uow;
        _spacesSeeder = spacesSeeder;
    }

    public async Task<CreateRetreatResponse> Handle(
        CreateRetreatCommand cmd,
        CancellationToken    ct)
    {
        if (await _repo.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, ct))
            throw new BusinessRuleException("Retreat already exists.");

        var retreat = new Retreat(
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

        await _repo.AddAsync(retreat, ct);
        await _uow.SaveChangesAsync(ct);
        
        const int DefaultMaxPeople = 8; 
        await _spacesSeeder.SeedDefaultsIfMissingAsync(retreat.Id, DefaultMaxPeople, ct);
        
         retreat.BumpServiceSpacesVersion();

        await _uow.SaveChangesAsync(ct); 

        return new CreateRetreatResponse(retreat.Id);
    }
}
