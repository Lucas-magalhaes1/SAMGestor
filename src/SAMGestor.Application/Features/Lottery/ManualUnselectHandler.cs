using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Lottery;

public sealed class ManualUnselectHandler(
    IRegistrationRepository regRepo,
    IUnitOfWork uow) 
    : IRequestHandler<ManualUnselectCommand, Unit>
{
    public async Task<Unit> Handle(ManualUnselectCommand cmd, CancellationToken ct)
    {
        var reg = await regRepo.GetByIdAsync(cmd.RegistrationId, ct)
                  ?? throw new NotFoundException(nameof(Registration), cmd.RegistrationId);

        if (reg.RetreatId != cmd.RetreatId)
            throw new BusinessRuleException("Registration does not belong to this retreat.");

        if (reg.Status == RegistrationStatus.Selected)
        {
            await regRepo.UpdateStatusesAsync(new[] { reg.Id }, RegistrationStatus.NotSelected, ct);
            await uow.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}