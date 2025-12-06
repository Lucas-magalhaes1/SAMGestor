using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Families.Create;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Features.Families.GetById;
using SAMGestor.Application.Features.Families.Lock;
using SAMGestor.Application.Features.Families.Reset;
using SAMGestor.Application.Features.Families.Unassigned;
using SAMGestor.Application.Features.Families.Update;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Family;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
[SwaggerTag("Operações relacionadas às famílias de um retiro")]
[Authorize(Policy = Policies.ReadOnly)]  
public class RetreatFamiliesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gera famílias persistindo no banco (MVP: 2M+2F), opcionalmente limpando as existentes.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families/generate")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
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
    /// (Admin,Gestor,Consultor)
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
    /// (Admin,Gestor,Consultor)
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
    /// (Admin,Gestor)
    /// </summary>
    [HttpPut("families")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
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
    
    /// <summary>
    /// Trava ou destrava todas as famílias do retiro.
    /// (Admin,Gestor)
    /// </summary>
    
    
    [HttpPost("families/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<LockFamiliesResponse>> Lock(
        [FromRoute] Guid retreatId,
        [FromBody]  LockFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockFamiliesCommand(retreatId, body.Lock), ct);
        return Ok(res);
    }
    
    /// <summary>
    ///  Trava ou destrava uma família específica do retiro.
    /// (Admin,Gestor)
    /// </summary>
    
    [HttpPost("families/{familyId:guid}/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<LockSingleFamilyResponse>> LockFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromBody]  LockFamilyRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockSingleFamilyCommand(retreatId, familyId, body.Lock), ct);
        return Ok(res);
    }
    
    /// <summary>
    ///  Deleta uma família específica do retiro.
    ///  (Admin,Gestor)
    /// </summary>
    
    [HttpDelete("families/{familyId:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> DeleteFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteFamilyCommand(retreatId, familyId), ct);
        return NoContent();
    }
    
    /// <summary>
    ///  Lista os participantes não atribuídos a nenhuma família, com filtros opcionais.
    ///  (Admin,Gestor,Consultor)
    /// </summary>
    
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
    
    /// <summary>
    ///  Reseta todas as famílias do retiro, com opção de forçar a remoção das travadas.
    ///  (Admin,Gestor)
    /// </summary>  
    
    [HttpPost("families/reset")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<ResetFamiliesResponse>> Reset(
        [FromRoute] Guid retreatId,
        [FromBody]  ResetFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new ResetFamiliesCommand(retreatId, body.ForceLockedFamilies), ct);
        return Ok(res);
    }
    
    /// <summary>
    ///  Cria uma nova família no retiro, retornando 422 se houver warnings sem IgnoreWarnings.
    ///  (Admin,Gestor)
    /// </summary>
    
    [HttpPost("create/families")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
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
