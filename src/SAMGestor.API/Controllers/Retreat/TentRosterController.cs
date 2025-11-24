using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;
using SAMGestor.Application.Features.Tents.TentRoster.AutoAssign;
using SAMGestor.Application.Features.Tents.TentRoster.Get;
using SAMGestor.Application.Features.Tents.TentRoster.Unassign;
using SAMGestor.Application.Features.Tents.TentRoster.Unassigned;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/retreats/{retreatId:guid}/tents/roster")]
[SwaggerTag("Operações relacionadas às alocações das barracas de um retiro.")]
public sealed class TentRosterController(IMediator mediator) : ControllerBase
{
    /// <summary>Snapshot do quadro de barracas (com membros e posições).</summary>
    [HttpGet]
    public async Task<ActionResult<GetTentRosterResponse>> Get(
        [FromRoute] Guid retreatId,
        CancellationToken ct = default)
    {
        var res = await mediator.Send(new GetTentRosterQuery(retreatId), ct);
        return Ok(res);
    }

    /// <summary>Lista participantes pagos e sem barraca.</summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<GetTentUnassignedResponse>> Unassigned(
        [FromRoute] Guid retreatId,
        [FromQuery] string? gender = null,   // "Male" | "Female" | null
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var res = await mediator.Send(new GetTentUnassignedQuery(retreatId, gender, search), ct);
        return Ok(res);
    }

    /// <summary>Remove a alocação do participante (fica sem barraca).</summary>
    [HttpPost("unassign")]
    public async Task<ActionResult<UnassignFromTentResponse>> Unassign(
        [FromRoute] Guid retreatId,
        [FromBody]  UnassignRequest body,
        CancellationToken ct = default)
    {
        var cmd = new UnassignFromTentCommand(
            RetreatId:       retreatId,
            RegistrationIds: new[] { body.RegistrationId } 
        );

        var res = await mediator.Send(cmd, ct);
        return Ok(res);
    }

    /// <summary>Distribui automaticamente pagos/sem-barraca nas barracas disponíveis.</summary>
    [HttpPost("auto-assign")]
    public async Task<ActionResult<AutoAssignTentsResponse>> AutoAssign(
        [FromRoute] Guid retreatId,
        [FromBody]  AutoAssignRequest body,
        CancellationToken ct = default)
    {
        var cmd = new AutoAssignTentsCommand(
            RetreatId:     retreatId,
            RespectLocked: body.RespectLocked
        );

        var res = await mediator.Send(cmd, ct);
        return Ok(res);
    }

    /// <summary>Salva o snapshot do quadro (mover pessoas, reordenar, etc.).</summary>
    [HttpPut]
    public async Task<ActionResult<UpdateTentRosterResponse>> Update(
        [FromRoute] Guid retreatId,
        [FromBody]  UpdateTentRosterCommand body,
        CancellationToken ct = default)
    {
        var cmd = body with { RetreatId = retreatId };
        var res = await mediator.Send(cmd, ct);
        return Ok(res);
    }
    
    public sealed record UnassignRequest(Guid RegistrationId);
    public sealed record AutoAssignRequest(bool RespectLocked = true);
}
