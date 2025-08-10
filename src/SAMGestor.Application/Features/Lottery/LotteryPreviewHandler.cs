using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Lottery;

public sealed class LotteryPreviewHandler(
    IRetreatRepository retreatRepo,
    IRegistrationRepository regRepo)
    : IRequestHandler<LotteryPreviewQuery, LotteryResultDto>
{
    public async Task<LotteryResultDto> Handle(LotteryPreviewQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        if (retreat.ContemplationClosed)
            throw new BusinessRuleException("Contemplation is closed for this retreat.");

        // capacidade atual por gênero
        var maleOcc = await regRepo.CountByStatusesAndGenderAsync(q.RetreatId, SlotPolicy.OccupyingStatuses, Gender.Male, ct);
        var femOcc  = await regRepo.CountByStatusesAndGenderAsync(q.RetreatId, SlotPolicy.OccupyingStatuses, Gender.Female, ct);

        var maleCap = Math.Max(0, retreat.MaleSlots   - maleOcc);
        var femCap  = Math.Max(0, retreat.FemaleSlots - femOcc);

        // pools candidatos: NotSelected + Enabled
        var malePool = await regRepo.ListAppliedIdsByGenderAsync(q.RetreatId, Gender.Male, ct);
        var femPool  = await regRepo.ListAppliedIdsByGenderAsync(q.RetreatId, Gender.Female, ct);

        Shuffler.ShuffleInPlace(malePool);
        Shuffler.ShuffleInPlace(femPool);

        var malePick = malePool.Take(Math.Min(maleCap, malePool.Count)).ToList();
        var femPick  = femPool.Take(Math.Min(femCap,  femPool.Count)).ToList();

        return new LotteryResultDto(malePick, femPick, maleCap, femCap);
    }
}