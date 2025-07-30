using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Retreats.GetAll;
using SAMGestor.Application.Features.Retreats.GetById;
using SAMGestor.Application.Features.Retreats.Update;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RetreatsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateRetreat(CreateRetreatCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById),
            new { id = result.RetreatId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await mediator.Send(new GetRetreatByIdQuery(id));
        return Ok(response);
    }
    
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var response = await mediator.Send(new ListRetreatsQuery(skip, take));
        return Ok(response);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRetreatCommand body)
    {
        var command = body with { Id = id };          
        var result  = await mediator.Send(command);
        return Ok(Response);
    }
}
