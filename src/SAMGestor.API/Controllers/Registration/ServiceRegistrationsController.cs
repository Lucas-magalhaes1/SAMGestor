using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Registrations.Confirmed;
using SAMGestor.Application.Features.Service.Registrations.Create;
using SAMGestor.Application.Features.Service.Registrations.GetById;
using SAMGestor.Application.Features.Service.Roster.Get;
using SAMGestor.Application.Features.Service.Roster.Unassigned;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Registration;

[ApiController]
[SwaggerTag("Operações relacionadas às inscrições de serviço para retiros.")]
[Route("api/retreats/{retreatId:guid}/service/registrations")]
public class ServiceRegistrationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    ///  Criar uma nova inscrição de serviço para um retiro específico.
    /// </summary>

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
    
    /// <summary>
    ///  Obter os detalhes de uma inscrição de serviço específica por ID.
    /// </summary>
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetServiceRegistrationResponse>> GetById(
        Guid retreatId,
        Guid id,
        CancellationToken ct)
    {
        var res = await mediator.Send(new GetServiceRegistrationQuery(retreatId, id), ct);
        return Ok(res);
    }
    
    /// <summary>
    ///  Obter a lista de membros de serviço atribuídos para um retiro específico.
    /// </summary>
    
    [HttpGet("roster")]
    public async Task<ActionResult<GetServiceRosterResponse>> GetRoster(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetServiceRosterQuery(retreatId), ct));
    
    /// <summary>
    ///  Obter a lista de membros de serviço não atribuídos para um retiro específico.
    /// </summary>
    
    
    [HttpGet("roster/unassigned")]
    public async Task<ActionResult<GetUnassignedServiceMembersResponse>> GetUnassigned(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetUnassignedServiceMembersQuery(retreatId), ct));
    
    /// <summary>
    ///  Obter a lista de inscrições de serviço confirmadas para um retiro específico.
    /// </summary>
    
    [HttpGet("confirmed")]
        public async Task<IActionResult> GetConfirmed([FromRoute] Guid retreatId, CancellationToken ct)
        {
            var result = await mediator.Send(new GetConfirmedServiceRegistrationsQuery(retreatId), ct);
            return Ok(result);
        }
}