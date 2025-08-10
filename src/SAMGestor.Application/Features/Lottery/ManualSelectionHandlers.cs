using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Application.Interfaces;
using System.Data;

namespace SAMGestor.Application.Features.Lottery;

public sealed class ManualSelectHandler(
    IRetreatRepository retreatRepo,
    IRegistrationRepository regRepo,
    IUnitOfWork uow)
    : IRequestHandler<ManualSelectCommand, Unit>
{
    public async Task<Unit> Handle(ManualSelectCommand cmd, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

            if (retreat.ContemplationClosed)
                throw new BusinessRuleException("Contemplation is closed for this retreat.");

            var reg = await regRepo.GetByIdAsync(cmd.RegistrationId, ct)
                ?? throw new NotFoundException(nameof(Registration), cmd.RegistrationId);

            if (reg.RetreatId != cmd.RetreatId)
                throw new BusinessRuleException("Registration does not belong to this retreat.");

            if (reg.Status != RegistrationStatus.NotSelected)
            {
                await uow.CommitTransactionAsync(ct);
                return Unit.Value;
            }

            var occupied = await regRepo.CountByStatusesAndGenderAsync(
                cmd.RetreatId, SlotPolicy.OccupyingStatuses, reg.Gender, ct);

            var cap = reg.Gender == Gender.Male ? retreat.MaleSlots : retreat.FemaleSlots;
            if (occupied >= cap)
                throw new BusinessRuleException("No slots available for this gender.");

            await regRepo.UpdateStatusesAsync(new[] { reg.Id }, RegistrationStatus.Selected, ct);

            await uow.CommitTransactionAsync(ct); // já salva + comita
            return Unit.Value;
        }
        catch
        {
            await uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
