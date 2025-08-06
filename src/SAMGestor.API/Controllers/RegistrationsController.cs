using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateRegistrationCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtRoute(
            nameof(GetById), new { id = result.RegistrationId }, result);
    }
    
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    public IActionResult GetById(Guid id) => NotFound();
    
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid retreatId, 
        [FromQuery] string? status = null,
        [FromQuery] string? region = null,
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 20)
    {
        var response = await mediator.Send(new GetAllRegistrationsQuery(retreatId, status, region, skip, take));
        return Ok(response);
    }
}