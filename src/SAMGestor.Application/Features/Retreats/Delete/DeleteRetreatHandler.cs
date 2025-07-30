using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Delete;

public sealed class DeleteRetreatHandler(IRetreatRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteRetreatCommand, DeleteRetreatResponse>
{
    public async Task<DeleteRetreatResponse> Handle(
        DeleteRetreatCommand cmd, CancellationToken ct)
    {
        var retreat = await repo.GetByIdAsync(cmd.Id, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.Id);

        // Futuro com fature implementada de incritos e contemplados ajustar para impedir remoção se houver confirmações

        await repo.RemoveAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        return new DeleteRetreatResponse(cmd.Id);
    }
}