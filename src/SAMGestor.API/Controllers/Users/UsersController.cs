using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Features.Users.BlockUser;
using SAMGestor.Application.Features.Users.Create;
using SAMGestor.Application.Features.Users.Delete;
using SAMGestor.Application.Features.Users.ForceChangeEmail;
using SAMGestor.Application.Features.Users.ForceChangePassword;
using SAMGestor.Application.Features.Users.GetById;
using SAMGestor.Application.Features.Users.GetCredentials;
using SAMGestor.Application.Features.Users.UnblockUser;
using SAMGestor.Application.Features.Users.Update;
using SAMGestor.Domain.Exceptions;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Users;

[ApiController]
[Route("api/users")]
[SwaggerTag("Operações relacionadas às contas de usuário.")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary> Detalhe de um usuário. (Admin,Gestor,Consultor - próprio perfil) </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
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

    /// <summary> Credenciais de um usuário. (Admin,Gestor)</summary>
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

    /// <summary> Cria um novo usuário e envia convite por e-mail. (Admin cria Gestor/Consultor, Gestor cria Consultor) </summary>
    [HttpPost]
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

    /// <summary> Atualiza dados básicos de um usuário. (Admin atualiza qualquer, usuário atualiza próprio perfil) </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]

public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest body, CancellationToken ct)
    {
        if (id != body.Id) return BadRequest(new { error = "ID da rota incompatível" });
        await _mediator.Send(new UpdateUserCommand(body.Id, body.Name, body.Phone), ct);
        return NoContent();
    }

    /// <summary> Exclui um usuário. (Admin) </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    /// <summary> Altera role de um usuário (não implementado). (Admin)</summary>
     
    [HttpPost("{id:guid}/roles")]
    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult ChangeRole([FromRoute] Guid id, [FromBody] ChangeRoleRequest body)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Alteração de função ainda não implementada" });
    }
    
    /// <summary> Força mudança de e-mail de um usuário. (Admin) </summary>
    [HttpPost("{id:guid}/force-change-email")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> ForceChangeEmail(
        [FromRoute] Guid id,
        [FromBody] ForceChangeEmailRequest body,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ForceChangeEmailCommand(id, body.NewEmail), ct);
            return Ok(new
            {
                message = "E-mail alterado com sucesso. Usuário receberá link de confirmação no novo e-mail"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary> Força mudança de senha de um usuário. (Admin) </summary>
    [HttpPost("{id:guid}/force-change-password")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> ForceChangePassword(
        [FromRoute] Guid id,
        [FromBody] ForceChangePasswordRequest body,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ForceChangePasswordCommand(id, body.NewPassword), ct);
            return Ok(new
            {
                message = "Senha alterada com sucesso. Todas as sessões do usuário foram encerradas"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    public sealed record ForceChangeEmailRequest(string NewEmail);
    public sealed record ForceChangePasswordRequest(string NewPassword);
    
    /// <summary> Bloqueia um usuário . (Admin)</summary>
    [HttpPost("{id:guid}/block")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> BlockUser([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new BlockUserCommand(id), ct);
            return Ok(new
            {
                message = "Usuário bloqueado com sucesso. Todas as sessões foram encerradas"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>  Desbloqueia um usuário (reabilita conta). (Admin) </summary>
    [HttpPost("{id:guid}/unblock")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> UnblockUser([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UnblockUserCommand(id), ct);
            return Ok(new
            {
                message = "Usuário desbloqueado com sucesso. Pode fazer login novamente"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    /// <summary>
    /// Lista todos os usuários (paginado).
    /// (Admin,Gestor)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<ListUsersResponse>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var res = await _mediator.Send(new ListUsersQuery(skip, take, search), ct);
        return Ok(res);
    }
}

