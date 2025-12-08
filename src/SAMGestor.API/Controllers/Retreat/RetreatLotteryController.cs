using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Lottery;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
[SwaggerTag("Operações relacionadas às inscrições em retiros. (Admin,Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]   
public class RetreatLotteryController : ControllerBase
{
    private readonly IMediator _mediator;
    public RetreatLotteryController(IMediator mediator) => _mediator = mediator;
    
    /// <summary>
    /// Realiza o preview do sorteio com suporte a prioridades por cidade e/ou faixa etária.
    /// </summary>
    /// <param name="retreatId">ID do retiro</param>
    /// <param name="request">Critérios de prioridade (opcionais)</param>
    [HttpPost("lottery/preview")]
    public async Task<ActionResult<LotteryResultDto>> Preview(
        [FromRoute] Guid retreatId,
        [FromBody] LotteryPreviewRequest? request,
        CancellationToken ct)
    {
        var query = new LotteryPreviewQuery(
            retreatId,
            request?.PriorityCities,
            request?.MinAge,
            request?.MaxAge
        );

        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
    
    /// <summary>
    /// Confirma o sorteio com suporte a prioridades por cidade e/ou faixa etária.
    /// </summary>
    /// <param name="retreatId">ID do retiro</param>
    /// <param name="request">Critérios de prioridade (opcionais)</param>
    [HttpPost("lottery/commit")]
    public async Task<ActionResult<LotteryResultDto>> Commit(
        [FromRoute] Guid retreatId,
        [FromBody] LotteryCommitRequest? request,
        CancellationToken ct)
    {
        var command = new LotteryCommitCommand(
            retreatId,
            request?.PriorityCities,
            request?.MinAge,
            request?.MaxAge
        );

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Realiza a seleção manual de uma inscrição para um retiro específico.
    /// </summary>
    [HttpPost("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualSelect(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid registrationId,
        CancellationToken ct)
    {
        await _mediator.Send(new ManualSelectCommand(retreatId, registrationId), ct);
        return NoContent();
    }
    
    /// <summary>
    /// Desfaz a seleção manual de uma inscrição para um retiro específico.
    /// </summary>
    [HttpDelete("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualUnselect(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid registrationId,
        CancellationToken ct)
    {
        await _mediator.Send(new ManualUnselectCommand(retreatId, registrationId), ct);
        return NoContent();
    }
}

// Request DTOs
public sealed record LotteryPreviewRequest(
    List<string>? PriorityCities = null,
    int? MinAge = null,
    int? MaxAge = null
);

public sealed record LotteryCommitRequest(
    List<string>? PriorityCities = null,
    int? MinAge = null,
    int? MaxAge = null
);
