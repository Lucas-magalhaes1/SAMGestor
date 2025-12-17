using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.Update;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    
    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork uow, ICurrentUser currentUser)
    {
        _users = users;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException("User not found");

        var role = _currentUser.Role?.ToLowerInvariant();
        var isAdmin = role is "administrator" or "admin";
        var isSelf = _currentUser.UserId == request.Id;

        if (!isAdmin && !isSelf)
            throw new ForbiddenException("Você só pode editar seu próprio perfil");

        u.ChangeName(new FullName(request.Name));
        u.ChangePhone(request.Phone);
        
        
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }


    private static Domain.Entities.User Update(Domain.Entities.User u, UpdateUserCommand r)
    {
        u = new Domain.Entities.User(new FullName(r.Name), u.Email, r.Phone, u.PasswordHash, u.Role);
        return u;
    }
}