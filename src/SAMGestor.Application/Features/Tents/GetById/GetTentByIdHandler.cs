using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.GetById;

public sealed class GetTentByIdHandler(
    ITentRepository tentRepo,
    IRegistrationRepository regRepo
) : IRequestHandler<GetTentByIdQuery, GetTentByIdResponse>
{
    public async Task<GetTentByIdResponse> Handle(GetTentByIdQuery q, CancellationToken ct)
    {
        var tent = await tentRepo.GetByIdAsync(q.TentId, ct)
                   ?? throw new NotFoundException(nameof(Tent), q.TentId);

        if (tent.RetreatId != q.RetreatId)
            throw new NotFoundException(nameof(Tent), q.TentId);

        var assigned = await regRepo.CountByTentAsync(tent.Id, ct);

        return new GetTentByIdResponse(
            TentId: tent.Id,
            RetreatId: tent.RetreatId,
            Number: tent.Number.Value.ToString(), // ← sem ?.
            Category: tent.Category,
            Capacity: tent.Capacity,
            IsActive: tent.IsActive,
            IsLocked: tent.IsLocked,
            Notes: tent.Notes,
            AssignedCount: assigned
        );
    }
}