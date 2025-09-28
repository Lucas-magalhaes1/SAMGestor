using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.Create;

public sealed class CreateServiceRegistrationHandler(
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IRetreatRepository retRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateServiceRegistrationCommand, CreateServiceRegistrationResponse>
{
    public async Task<CreateServiceRegistrationResponse> Handle(
        CreateServiceRegistrationCommand cmd,
        CancellationToken ct)
    {
        
        var retreat = await retRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!retreat.RegistrationWindowOpen(today))
            throw new BusinessRuleException("Registration period closed.");
        
        if (await regRepo.IsCpfBlockedAsync(cmd.Cpf, ct))
            throw new BusinessRuleException("CPF is blocked.");

        
        if (await regRepo.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, ct))
            throw new BusinessRuleException("CPF already registered for this retreat (Serve).");

        if (await regRepo.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, ct))
            throw new BusinessRuleException("Email already registered for this retreat (Serve).");

        
        var hasActive = await spaceRepo.HasActiveByRetreatAsync(cmd.RetreatId, ct);
        if (hasActive && cmd.PreferredSpaceId is null)
            throw new BusinessRuleException("Preferred space is required.");

        if (cmd.PreferredSpaceId is not null)
        {
            var space = await spaceRepo.GetByIdAsync(cmd.PreferredSpaceId.Value, ct);
            if (space is null || space.RetreatId != cmd.RetreatId)
                throw new BusinessRuleException("Preferred space not found for this retreat.");
            if (!space.IsActive)
                throw new BusinessRuleException("Preferred space is inactive.");
        }
        
        var entity = new ServiceRegistration(
            cmd.RetreatId, cmd.Name, cmd.Cpf, cmd.Email, cmd.Phone,
            cmd.BirthDate, cmd.Gender, cmd.City, cmd.Region, cmd.PreferredSpaceId
        );

        await regRepo.AddAsync(entity, ct);

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            throw new BusinessRuleException("CPF or e-mail already registered for this retreat.");
        }

        return new CreateServiceRegistrationResponse(entity.Id);
    }
}
