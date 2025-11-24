using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Alerts.GetAll;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Service;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/alerts")]
[SwaggerTag("Operações relacionadas aos alertas de serviço.")]
public class ServiceAlertsController(IMediator mediator) : ControllerBase
{
    /// <summary> Lista os alertas de serviço para um retiro. </summary>
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