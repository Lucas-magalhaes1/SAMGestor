using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.RemovePhoto;

public sealed class RemoveUserPhotoHandler : IRequestHandler<RemoveUserPhotoCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RemoveUserPhotoHandler> _logger;

    public RemoveUserPhotoHandler(
        IUserRepository users,
        IStorageService storage,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<RemoveUserPhotoHandler> logger)
    {
        _users = users;
        _storage = storage;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(RemoveUserPhotoCommand cmd, CancellationToken ct)
    {
     
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;

        var isAdmin = _currentUser.Role?.ToLowerInvariant() is "administrator" or "admin";
        var isSelf = cmd.UserId == currentUserId;

        if (!isSelf && !isAdmin)
        {
            _logger.LogWarning(
                "Usuário {CurrentUserId} tentou remover foto de {TargetUserId} sem permissão",
                currentUserId, cmd.UserId);
            throw new ForbiddenException("Você só pode remover sua própria foto");
        }

        var user = await _users.GetByIdForUpdateAsync(cmd.UserId, ct);
        if (user is null)
            throw new NotFoundException("User", cmd.UserId);

        if (!user.HasProfilePhoto())
        {
            _logger.LogInformation("Usuário {UserId} não possui foto de perfil", user.Id);
            return Unit.Value;
        }
        
        if (!string.IsNullOrWhiteSpace(user.PhotoStorageKey))
        {
            try
            {
                await _storage.DeleteAsync(user.PhotoStorageKey, ct);
                _logger.LogInformation("Foto deletada do storage: {StorageKey}", user.PhotoStorageKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao deletar foto do storage: {StorageKey}", user.PhotoStorageKey);
               
            }
        }

        user.RemoveProfilePhoto();
        
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Foto de perfil removida: User={UserId}, RemovidaPor={CurrentUserId}",
            user.Id, currentUserId);

        return Unit.Value;
    }
}
