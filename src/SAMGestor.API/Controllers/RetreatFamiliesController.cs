using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Families.Create;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Features.Families.GetById;
using SAMGestor.Application.Features.Families.Lock;
using SAMGestor.Application.Features.Families.Reset;
using SAMGestor.Application.Features.Families.Unassigned;
using SAMGestor.Application.Features.Families.Update;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
public class RetreatFamiliesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gera famílias persistindo no banco (MVP: 2M+2F), opcionalmente limpando as existentes.
    /// </summary>
    [HttpPost("families/generate")]
    public async Task<ActionResult<GenerateFamiliesResponse>> Generate(
        [FromRoute] Guid retreatId,
        [FromBody] GenerateFamiliesCommand body,
        CancellationToken ct)
    {
        var cmd    = body with { RetreatId = retreatId };
        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista as famílias do retiro (com métricas e alertas).
    /// </summary>
    [HttpGet("families")]
    public async Task<ActionResult<GetAllFamiliesResponse>> List(
        [FromRoute] Guid retreatId,
        [FromQuery] bool includeAlerts = true,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllFamiliesQuery(retreatId, includeAlerts), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtém uma família específica do retiro (com métricas e alertas).
    /// </summary>
    [HttpGet("families/{familyId:guid}")]
    public async Task<ActionResult<GetFamilyByIdResponse>> GetById(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromQuery] bool includeAlerts = true,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFamilyByIdQuery(retreatId, familyId, includeAlerts), ct);
        return Ok(result);
    }

    /// <summary>
    /// Salva o snapshot (drag-and-drop) das famílias. Retorna 422 se houver erros
    /// ou warnings sem IgnoreWarnings.
    /// </summary>
    [HttpPut("families")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid retreatId,
        [FromBody]  UpdateFamiliesCommand body,
        CancellationToken ct)
    {
        var cmd    = body with { RetreatId = retreatId };
        var result = await mediator.Send(cmd, ct);

        // Convenção: 422 quando não foi possível persistir (Errors) ou quando há Warnings e IgnoreWarnings=false
        var hasErrors   = result.Errors is not null && result.Errors.Count > 0;
        var hasWarnings = result.Warnings is not null && result.Warnings.Count > 0;
        
        var didPersist = result.Families is not null && result.Families.Count > 0;

        if (!didPersist && (hasErrors || hasWarnings))
            return UnprocessableEntity(result);

        return Ok(result);
    }
    
    [HttpPost("families/lock")]
    public async Task<ActionResult<LockFamiliesResponse>> Lock(
        [FromRoute] Guid retreatId,
        [FromBody]  LockFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockFamiliesCommand(retreatId, body.Lock), ct);
        return Ok(res);
    }
    
    [HttpPost("families/{familyId:guid}/lock")]
    public async Task<ActionResult<LockSingleFamilyResponse>> LockFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromBody]  LockFamilyRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockSingleFamilyCommand(retreatId, familyId, body.Lock), ct);
        return Ok(res);
    }
    
    [HttpDelete("families/{familyId:guid}")]
    public async Task<IActionResult> DeleteFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteFamilyCommand(retreatId, familyId), ct);
        return NoContent();
    }
    
    [HttpGet("families/unassigned")]
    public async Task<ActionResult<GetUnassignedResponse>> Unassigned(
        [FromRoute] Guid retreatId,
        [FromQuery] string? gender = null,
        [FromQuery] string? city = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var res = await mediator.Send(new GetUnassignedQuery(retreatId, gender, city, search), ct);
        return Ok(res);
    }
    
    [HttpPost("families/reset")]
    public async Task<ActionResult<ResetFamiliesResponse>> Reset(
        [FromRoute] Guid retreatId,
        [FromBody]  ResetFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new ResetFamiliesCommand(retreatId, body.ForceLockedFamilies), ct);
        return Ok(res);
    }
    
    [HttpPost("create/families")]
    public async Task<IActionResult> CreateFamily(
        [FromRoute] Guid retreatId,
        [FromBody]  CreateFamilyRequest body,
        CancellationToken ct)
    {
        var cmd = new CreateFamilyCommand(
            RetreatId: retreatId,
            Name: body.Name,
            MemberIds: body.MemberIds ?? Array.Empty<Guid>(),
            IgnoreWarnings: body.IgnoreWarnings
        );

        var result = await mediator.Send(cmd, ct);

        if (!result.Created)
        {
            // 422 com os warnings (sem persistir)
            return UnprocessableEntity(new
            {
                version = result.Version,
                warnings = result.Warnings
            });
        }

        // 201 Created
        return CreatedAtAction(
            nameof(GetById),               // seu GET /families/{familyId}
            routeValues: new { retreatId, familyId = result.FamilyId },
            value: new
            {
                familyId = result.FamilyId,
                version  = result.Version,
                warnings = result.Warnings
            });
    }

    public sealed record ResetFamiliesRequest(bool ForceLockedFamilies);
    public sealed record LockFamiliesRequest(bool Lock);
    public sealed record LockFamilyRequest(bool Lock);
}
