using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.UploadPhoto;

public sealed class UploadUserPhotoHandler : IRequestHandler<UploadUserPhotoCommand, UploadUserPhotoResult>
{
    private readonly IUserRepository _users;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UploadUserPhotoHandler> _logger;

    public UploadUserPhotoHandler(
        IUserRepository users,
        IStorageService storage,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<UploadUserPhotoHandler> logger)
    {
        _users = users;
        _storage = storage;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UploadUserPhotoResult> Handle(UploadUserPhotoCommand cmd, CancellationToken ct)
    {
        
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;

       
        var isAdmin = _currentUser.Role?.ToLowerInvariant() is "administrator" or "admin";
        var isSelf = cmd.UserId == currentUserId;

        if (!isSelf && !isAdmin)
        {
            _logger.LogWarning(
                "Usuário {CurrentUserId} tentou alterar foto de {TargetUserId} sem permissão",
                currentUserId, cmd.UserId);
            throw new ForbiddenException("Você só pode alterar sua própria foto");
        }

      
        var user = await _users.GetByIdForUpdateAsync(cmd.UserId, ct);
        if (user is null)
            throw new NotFoundException("User", cmd.UserId);

       
        if (cmd.FileStream is null || cmd.FileSizeBytes == 0)
            throw new ArgumentException("Arquivo é obrigatório");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException(
                $"Formato não permitido. Use: {string.Join(", ", allowedExtensions)}");
        }

        const long maxSizeBytes = 2 * 1024 * 1024; 
        if (cmd.FileSizeBytes > maxSizeBytes)
            throw new ArgumentException("Arquivo muito grande. Tamanho máximo: 2MB");

        var allowedContentTypes = new[] { "image/jpeg", "image/png" };
        if (!allowedContentTypes.Contains(cmd.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Tipo de arquivo não permitido. Use JPG ou PNG");
        }
        
        if (user.HasProfilePhoto() && !string.IsNullOrWhiteSpace(user.PhotoStorageKey))
        {
            try
            {
                await _storage.DeleteAsync(user.PhotoStorageKey, ct);
                _logger.LogInformation("Foto anterior deletada: {StorageKey}", user.PhotoStorageKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao deletar foto anterior: {StorageKey}", user.PhotoStorageKey);
               
            }
        }
        
        var storageKey = $"users/{user.Id}/profile-photo{fileExtension}";
        
        var (savedKey, sizeBytes) = await _storage.SaveAsync(
            cmd.FileStream, 
            storageKey, 
            cmd.ContentType, 
            ct);
        
        user.SetProfilePhoto(savedKey, cmd.ContentType, sizeBytes);
        
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Foto de perfil atualizada: User={UserId}, Size={SizeKB}KB, AlteradoPor={CurrentUserId}",
            user.Id, sizeBytes / 1024, currentUserId);

        return new UploadUserPhotoResult(
            UserId: user.Id,
            StorageKey: savedKey,
            ContentType: cmd.ContentType,
            SizeBytes: sizeBytes,
            UploadedAt: user.PhotoUploadedAt!.Value
        );
    }
}
