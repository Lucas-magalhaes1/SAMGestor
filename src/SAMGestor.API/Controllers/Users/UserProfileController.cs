using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Users.GetById;
using SAMGestor.Application.Features.Users.UploadPhoto;
using SAMGestor.Application.Features.Users.RemovePhoto;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Users;

[ApiController]
[Route("api/users/me")]
[Authorize(Policy = Policies.Authenticated)]
[SwaggerTag("Operações de perfil do usuário logado (qualquer role).")]
public class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storage; 

    public UserProfileController(
        IMediator mediator, 
        ICurrentUser currentUser,
        IStorageService storage)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _storage = storage; 
    }
    
    /// <summary>
    /// Retorna informações completas do usuário logado (próprio perfil).
    /// </summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Obter perfil completo",
        Description = "Retorna todas as informações do perfil do usuário autenticado, incluindo foto, última sessão, etc."
    )]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;
    
        try
        {
            var result = await _mediator.Send(new GetUserByIdQuery(userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Usuário não encontrado" });
        }
    }

    /// <summary>
    /// Upload ou atualiza foto de perfil do usuário logado.
    /// </summary>
    [HttpPost("photo")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload foto de perfil",
        Description = "Permite que o usuário autenticado faça upload ou atualize sua foto de perfil. Formatos aceitos: JPG, PNG (máx 2MB)."
    )]
    public async Task<IActionResult> UploadPhoto(
        [FromForm] UploadPhotoRequest request,
        CancellationToken ct)
    {
        if (request.PhotoFile is null || request.PhotoFile.Length == 0)
            return BadRequest(new { error = "Arquivo é obrigatório" });

        var userId = _currentUser.UserId!.Value;

        await using var stream = request.PhotoFile.OpenReadStream();
        
        var command = new UploadUserPhotoCommand(
            UserId: userId,
            FileStream: stream,
            FileName: request.PhotoFile.FileName,
            ContentType: request.PhotoFile.ContentType,
            FileSizeBytes: request.PhotoFile.Length
        );

        var result = await _mediator.Send(command, ct);
        
        var publicUrl = _storage.GetPublicUrl(result.StorageKey);

        return Ok(new
        {
            message = "Foto atualizada com sucesso",
            photoUrl = publicUrl,
            uploadedAt = result.UploadedAt
        });
    }

    /// <summary>
    /// Remove foto de perfil do usuário logado.
    /// </summary>
    [HttpDelete("photo")]
    [SwaggerOperation(
        Summary = "Remove foto de perfil",
        Description = "Remove a foto de perfil do usuário autenticado."
    )]
    public async Task<IActionResult> RemovePhoto(CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;

        var command = new RemoveUserPhotoCommand(userId);
        await _mediator.Send(command, ct);

        return Ok(new { message = "Foto removida com sucesso" });
    }

    /// <summary>
    /// Retorna a foto de perfil do usuário logado.
    /// </summary>
    [HttpGet("photo")]
    [SwaggerOperation(
        Summary = "Obter foto de perfil",
        Description = "Retorna o arquivo da foto de perfil do usuário autenticado."
    )]
    public async Task<IActionResult> GetPhoto(
        [FromServices] IUserRepository userRepo,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;
        var user = await userRepo.GetByIdAsync(userId, ct);

        if (user is null || !user.HasProfilePhoto())
            return NotFound(new { error = "Foto não encontrada" });

        var filePath = Path.Combine("wwwroot/uploads", user.PhotoStorageKey!.Replace('/', Path.DirectorySeparatorChar));
        
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Arquivo não encontrado" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        return File(fileBytes, user.PhotoContentType ?? "image/jpeg");
    }
    
    /// <summary>
    /// Admin: Upload foto de qualquer usuário.
    /// </summary>
    [HttpPost("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SwaggerOperation(
        Summary = "Admin: Upload foto de usuário",
        Description = "Permite que administradores façam upload da foto de perfil de qualquer usuário."
    )]
    public async Task<IActionResult> AdminUploadUserPhoto(
        [FromRoute] Guid id,
        [FromForm] UploadPhotoRequest request,
        CancellationToken ct)
    {
        if (request.PhotoFile is null || request.PhotoFile.Length == 0)
            return BadRequest(new { error = "Arquivo é obrigatório" });

        await using var stream = request.PhotoFile.OpenReadStream();
    
        var command = new UploadUserPhotoCommand(
            UserId: id,
            FileStream: stream,
            FileName: request.PhotoFile.FileName,
            ContentType: request.PhotoFile.ContentType,
            FileSizeBytes: request.PhotoFile.Length
        );

        var result = await _mediator.Send(command, ct);
        
        var publicUrl = _storage.GetPublicUrl(result.StorageKey);

        return Ok(new
        {
            message = "Foto atualizada com sucesso",
            userId = result.UserId,
            photoUrl = publicUrl, 
            uploadedAt = result.UploadedAt
        });
    }

    /// <summary>
    /// Admin: Remove foto de qualquer usuário.
    /// </summary>
    [HttpDelete("{id:guid}/photo")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SwaggerOperation(
        Summary = "Admin: Remove foto de usuário",
        Description = "Permite que administradores removam a foto de perfil de qualquer usuário."
    )]
    public async Task<IActionResult> AdminRemoveUserPhoto(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new RemoveUserPhotoCommand(id);
        await _mediator.Send(command, ct);

        return Ok(new { message = "Foto removida com sucesso" });
    }

    public sealed class UploadPhotoRequest
    {
        public IFormFile PhotoFile { get; set; } = null!;
    }
}
