using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Retreats.Create;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RetreatsController : ControllerBase
{
    private readonly IMediator _mediator;
    public RetreatsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateRetreat(CreateRetreatCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById),
            new { id = result.RetreatId }, result);
    }

    // Placeholder – future query handler
    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => NotFound();
}