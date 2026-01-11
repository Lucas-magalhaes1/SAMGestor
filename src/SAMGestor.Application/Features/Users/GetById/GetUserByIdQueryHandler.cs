using MediatR;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.GetById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetail>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStorageService _storage; 
    
    public GetUserByIdQueryHandler(
        IUserRepository users, 
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStorageService storage)
    {
        _users = users;
        _currentUser = currentUser;
        _clock = clock;
        _storage = storage;
    }

    public async Task<UserDetail> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(request.Id, ct);
        if (u is null) throw new KeyNotFoundException("User not found");

        if (_currentUser.Role == "consultant" && _currentUser.UserId != request.Id)
        {
            throw new ForbiddenException("Você só pode visualizar seu próprio perfil");
        }

        var now = _clock.UtcNow;
        
        string? photoUrl = null;
        if (u.HasProfilePhoto() && !string.IsNullOrWhiteSpace(u.PhotoStorageKey))
        {
            photoUrl = _storage.GetPublicUrl(u.PhotoStorageKey);
        }
        
        return new UserDetail(
            u.Id,
            u.Name.Value,
            u.Email.Value,
            u.Phone,
            u.Role.ToShortName(),
            u.EmailConfirmed,
            u.Enabled,                    
            u.IsLocked(now),              
            u.LockoutEndAt,               
            u.LastLoginAt,
            photoUrl 
        );
    }
}
