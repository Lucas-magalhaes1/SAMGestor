using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using System.Data;
using SAMGestor.Application.Interfaces;

namespace SAMGestor.Application.Features.Lottery;

public sealed class LotteryCommitHandler(
    IMediator mediator,
    IUnitOfWork uow,
    IRetreatRepository retreatRepo,
    IRegistrationRepository regRepo)
    : IRequestHandler<LotteryCommitCommand, LotteryResultDto>
{
    public async Task<LotteryResultDto> Handle(LotteryCommitCommand cmd, CancellationToken ct)
    {
        // IMPORTANTE: faremos commit em transação SERIALIZABLE na infra (passo 3)
        await uow.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // revalida retiro
            var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                          ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
            if (retreat.ContemplationClosed)
                throw new BusinessRuleException("Contemplation is closed for this retreat.");

            // calcula preview dentro da mesma transação
            var preview = await mediator.Send(new LotteryPreviewQuery(cmd.RetreatId), ct);

            // aplica
            var toSelect = preview.Male.Concat(preview.Female).Distinct().ToList();
            if (toSelect.Count > 0)
                await regRepo.UpdateStatusesAsync(toSelect, Domain.Enums.RegistrationStatus.Selected, ct);

            await uow.SaveChangesAsync(ct);
            await uow.CommitTransactionAsync(ct);

            return preview;
        }
        catch
        {
            await uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}