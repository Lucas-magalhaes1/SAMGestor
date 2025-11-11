using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.Delete;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public DeleteUserCommandHandler(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.Id, ct)
                   ?? throw new KeyNotFoundException("User not found");

        await _users.DeleteAsync(user, ct);  
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}