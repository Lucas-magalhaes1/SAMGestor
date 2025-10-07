using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Registrations.Create;
using SAMGestor.Application.Features.Service.Registrations.GetById;
using SAMGestor.Application.Features.Service.Roster.Get;
using SAMGestor.Application.Features.Service.Roster.Unassigned;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/registrations")]
public class ServiceRegistrationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateServiceRegistrationResponse>> Create(
        Guid retreatId,
        [FromBody] CreateServiceRegistrationCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId };
        var res = await mediator.Send(cmd, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { retreatId, id = res.ServiceRegistrationId },
            res);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetServiceRegistrationResponse>> GetById(
        Guid retreatId,
        Guid id,
        CancellationToken ct)
    {
        var res = await mediator.Send(new GetServiceRegistrationQuery(retreatId, id), ct);
        return Ok(res);
    }
    
    [HttpGet("roster")]
    public async Task<ActionResult<GetServiceRosterResponse>> GetRoster(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetServiceRosterQuery(retreatId), ct));
    
    [HttpGet("roster/unassigned")]
    public async Task<ActionResult<GetUnassignedServiceMembersResponse>> GetUnassigned(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetUnassignedServiceMembersQuery(retreatId), ct));
}