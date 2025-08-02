using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetById;

public sealed class GetRetreatByIdHandler(IRetreatRepository repo)
    : IRequestHandler<GetRetreatByIdQuery, GetRetreatByIdResponse>
{
    public async Task<GetRetreatByIdResponse> Handle(
        GetRetreatByIdQuery query,
        CancellationToken   ct)
    {
        var retreat = await repo.GetByIdAsync(query.Id, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), query.Id);

        return new GetRetreatByIdResponse(
            retreat.Id,
            (string)retreat.Name,         
            retreat.Edition,
            retreat.Theme,
            retreat.StartDate,
            retreat.EndDate,
            retreat.MaleSlots,
            retreat.FemaleSlots,
            retreat.RegistrationStart,
            retreat.RegistrationEnd,
            retreat.FeeFazer.Amount,
            retreat.FeeServir.Amount,
            retreat.WestRegionPercentage.Value,
            retreat.OtherRegionsPercentage.Value);
    }
}