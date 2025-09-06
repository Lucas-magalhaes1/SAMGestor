using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Features.Families.GetById;
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

        // Se não salvou, o handler retorna Families vazio junto com Errors/Warn.
        var didPersist = result.Families is not null && result.Families.Count > 0;

        if (!didPersist && (hasErrors || hasWarnings))
            return UnprocessableEntity(result);

        return Ok(result);
    }
}
