using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.Update;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(request.Id, ct) ?? throw new KeyNotFoundException("User not found");
        u = Update(u, request);
        await _users.UpdateAsync(u, ct);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static Domain.Entities.User Update(Domain.Entities.User u, UpdateUserCommand r)
    {
        // FullName é VO imutável: recria
        u = new Domain.Entities.User(new FullName(r.Name), u.Email, r.Phone, u.PasswordHash, u.Role);
        return u;
    }
}