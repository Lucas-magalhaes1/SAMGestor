using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Lottery;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
[SwaggerTag("Operações relacionadas às inscrições em retiros.")]
public class RetreatLotteryController : ControllerBase
{
    private readonly IMediator _mediator;
    public RetreatLotteryController(IMediator mediator) => _mediator = mediator;
    
    /// <summary>
    ///  Realiza o sorteio das inscrições para um retiro específico.
    /// </summary>

    // PREVIEW do sorteio (não persiste nada)
    [HttpPost("lottery/preview")]
    public async Task<ActionResult<LotteryResultDto>> Preview([FromRoute] Guid retreatId, CancellationToken ct)
    {
        var result = await _mediator.Send(new LotteryPreviewQuery(retreatId), ct);
        return Ok(result);
    }
    
    /// <summary>
    ///  Confirma o sorteio das inscrições para um retiro específico, aplicando as seleções.
    /// </summary>

    // COMMIT do sorteio (aplica seleção)
    [HttpPost("lottery/commit")]
    public async Task<ActionResult<LotteryResultDto>> Commit([FromRoute] Guid retreatId, CancellationToken ct)
    {
        var result = await _mediator.Send(new LotteryCommitCommand(retreatId), ct);
        return Ok(result);
    }

    /// <summary>
    ///  Realiza a seleção manual de uma inscrição para um retiro específico.
    /// </summary>
    
    // Seleção manual (contemplar)
    [HttpPost("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualSelect([FromRoute] Guid retreatId, [FromRoute] Guid registrationId, CancellationToken ct)
    {
        await _mediator.Send(new ManualSelectCommand(retreatId, registrationId), ct);
        return NoContent();
    }
    
    /// <summary>
    ///  Desfaz a seleção manual de uma inscrição para um retiro específico.
    /// </summary>

    // Desfazer seleção manual
    [HttpDelete("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualUnselect([FromRoute] Guid retreatId, [FromRoute] Guid registrationId, CancellationToken ct)
    {
        await _mediator.Send(new ManualUnselectCommand(retreatId, registrationId), ct);
        return NoContent();
    }
}