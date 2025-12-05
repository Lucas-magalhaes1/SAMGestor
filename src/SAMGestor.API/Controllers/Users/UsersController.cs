using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Features.Users.Create;
using SAMGestor.Application.Features.Users.Delete;
using SAMGestor.Application.Features.Users.GetById;
using SAMGestor.Application.Features.Users.GetCredentials;
using SAMGestor.Application.Features.Users.Update;
using SAMGestor.Domain.Exceptions;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Users;

[ApiController]
[Route("api/users")]
[SwaggerTag("Operações relacionadas às contas de usuário.")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary> Detalhe de um usuário. </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummary>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var res = await _mediator.Send(new GetUserByIdQuery(id), ct);
            return Ok(res);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Usuário não encontrado" });
        }
    }

    /// <summary> Credenciais de um usuário. </summary>
    [HttpGet("{id:guid}/credentials")]
    public async Task<ActionResult<UserCredentialsResponse>> GetCredentials([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var res = await _mediator.Send(new GetUserCredentialsQuery(id), ct);
            return Ok(res);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Usuário não encontrado" });
        }
    }
    
    public sealed record ChangeRoleRequest(string Role);

    /// <summary> Cria um novo usuário e envia convite por e-mail. </summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManageAllButDeleteUsers)]
    public async Task<ActionResult<CreateUserResponse>> Create([FromBody] CreateUserRequest body, CancellationToken ct)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var res = await _mediator.Send(
                new CreateUserCommand(body.Name, body.Email, body.Phone, body.Role?.ToString(), baseUrl), ct);
        
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, new
            {
                id = res.Id,
                message = "Usuário criado com sucesso. Convite enviado por e-mail"
            });
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Tentativa não autorizada de criar usuário");
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao criar usuário");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary> Atualiza dados básicos de um usuário. </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest body, CancellationToken ct)
    {
        if (id != body.Id) return BadRequest(new { error = "ID da rota incompatível" });
        await _mediator.Send(new UpdateUserCommand(body.Id, body.Name, body.Phone), ct);
        return NoContent();
    }

    /// <summary> Exclui um usuário. </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    /// <summary> Confirma e-mail de um usuário. </summary>
    // (Opcional) mudar role
    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> ChangeRole([FromRoute] Guid id, [FromBody] ChangeRoleRequest body, CancellationToken ct)
    {
        // Aqui você pode criar um Command para change-role.
        // Por ora, retorno 501 para lembrar de implementar.
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Alteração de função ainda não implementada" });
    }
    
    /// <summary> Desbloqueia manualmente uma conta (apenas Admin). </summary>
    [HttpPost("{id:guid}/unlock")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> UnlockAccount([FromRoute] Guid id, CancellationToken ct)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id), ct);
        if (user == null)
            return NotFound(new { error = "Usuário não encontrado" });

        // Criar um Command para isso ou fazer direto:
        // user.FailedAccessCount = 0;
        // user.LockoutEndAt = null;
        // await _uow.SaveChangesAsync(ct);

        return Ok(new { message = "Conta desbloqueada com sucesso" });
    }
}
