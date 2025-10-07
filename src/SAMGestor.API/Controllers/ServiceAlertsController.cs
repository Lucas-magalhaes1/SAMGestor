using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Alerts.GetAll;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/alerts")]
public class ServiceAlertsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetServiceAlertsResponse>> Get(
        Guid retreatId, [FromQuery] string? mode, CancellationToken ct)
    {
        var parsed = mode?.ToLowerInvariant() switch
        {
            "preferences" => ServiceAlertMode.Preferences,
            "roster"      => ServiceAlertMode.Roster,
            _             => ServiceAlertMode.All
        };

        var res = await mediator.Send(new GetServiceAlertsQuery(retreatId, parsed), ct);
        return Ok(res);
    }
}