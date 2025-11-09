using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Lottery;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
public class RetreatLotteryController : ControllerBase
{
    private readonly IMediator _mediator;
    public RetreatLotteryController(IMediator mediator) => _mediator = mediator;

    // PREVIEW do sorteio (não persiste nada)
    [HttpPost("lottery/preview")]
    public async Task<ActionResult<LotteryResultDto>> Preview([FromRoute] Guid retreatId, CancellationToken ct)
    {
        var result = await _mediator.Send(new LotteryPreviewQuery(retreatId), ct);
        return Ok(result);
    }

    // COMMIT do sorteio (aplica seleção)
    [HttpPost("lottery/commit")]
    public async Task<ActionResult<LotteryResultDto>> Commit([FromRoute] Guid retreatId, CancellationToken ct)
    {
        var result = await _mediator.Send(new LotteryCommitCommand(retreatId), ct);
        return Ok(result);
    }

    // Seleção manual (contemplar)
    [HttpPost("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualSelect([FromRoute] Guid retreatId, [FromRoute] Guid registrationId, CancellationToken ct)
    {
        await _mediator.Send(new ManualSelectCommand(retreatId, registrationId), ct);
        return NoContent();
    }

    // Desfazer seleção manual
    [HttpDelete("selections/{registrationId:guid}")]
    public async Task<IActionResult> ManualUnselect([FromRoute] Guid retreatId, [FromRoute] Guid registrationId, CancellationToken ct)
    {
        await _mediator.Send(new ManualUnselectCommand(retreatId, registrationId), ct);
        return NoContent();
    }
}