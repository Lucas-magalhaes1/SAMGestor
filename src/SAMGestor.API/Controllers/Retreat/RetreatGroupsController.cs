using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Families.Groups.ListByStatus;
using SAMGestor.Application.Features.Families.Groups.Resend;
using SAMGestor.Application.Features.Families.Groups.RetryFailed;
using SAMGestor.Application.Features.Families.Groups.Status;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("admin/retreats/{retreatId:guid}/groups")]
public sealed class RetreatGroupsController(IMediator mediator) : ControllerBase
{
    /// <summary>Resumo por status (None/Creating/Active/Failed) para um retiro.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<GetGroupsStatusSummaryResponse>> GetStatusSummary(
        [FromRoute] Guid retreatId,
        CancellationToken ct)
    {
        var res = await mediator.Send(new GetGroupsStatusSummaryQuery(retreatId), ct);
        return Ok(res);
    }

    /// <summary>Lista famílias por status (none|creating|active|failed). Se vazio, lista todas.</summary>
    [HttpGet]
    public async Task<ActionResult<ListFamiliesByGroupStatusResponse>> ListByStatus(
        [FromRoute] Guid retreatId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var res = await mediator.Send(new ListFamiliesByGroupStatusQuery(retreatId, status), ct);
        return Ok(res);
    }

    /// <summary>Reenviar link do grupo existente (notify-only, não recria).</summary>
    [HttpPost("{familyId:guid}/resend")]
    public async Task<ActionResult<ResendFamilyGroupResponse>> Resend(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        CancellationToken ct)
    {
        var cmd = new ResendFamilyGroupCommand(retreatId, familyId);
        var res = await mediator.Send(cmd, ct);
        return res.Queued ? Accepted(res) : BadRequest(res);
    }

    public sealed record ResendFamilyGroupRequest(IReadOnlyList<string> Channels);

    /// <summary>Reprocessa todas as famílias com GroupStatus=Failed (recria grupos).</summary>
    [HttpPost("retry-failed")]
    public async Task<ActionResult<RetryFailedGroupsResponse>> RetryFailed(
        [FromRoute] Guid retreatId,
        CancellationToken ct)
    {
        var res = await mediator.Send(new RetryFailedGroupsCommand(retreatId), ct);
        return Accepted(res);
    }
}
