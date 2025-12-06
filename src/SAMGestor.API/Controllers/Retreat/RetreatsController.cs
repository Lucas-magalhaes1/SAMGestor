using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Retreats.Delete;
using SAMGestor.Application.Features.Retreats.GetAll;
using SAMGestor.Application.Features.Retreats.GetById;
using SAMGestor.Application.Features.Retreats.Update;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Operações relacionadas aos retiros")]
[Authorize(Policy = Policies.ReadOnly)]
public class RetreatsController(IMediator mediator) : ControllerBase
{
    
    /// <summary>
    ///  Cria um novo retiro.
    ///  (Admin,Gestor)
    /// </summary>
    
    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> CreateRetreat(CreateRetreatCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById),
            new { id = result.RetreatId }, result);
    }

    /// <summary>
    /// Obtém os detalhes de um retiro específico pelo seu ID.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await mediator.Send(new GetRetreatByIdQuery(id));
        return Ok(response);
    }
    
    /// <summary>
    /// Lista todos os retiros com paginação.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var response = await mediator.Send(new ListRetreatsQuery(skip, take));
        return Ok(response);
    }
    
    /// <summary>
    /// Atualiza os detalhes de um retiro existente.
    /// (Admin,Gestor)
    /// </summary>
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> Update(Guid id, UpdateRetreatCommand body)
    {
        var command = body with { Id = id };          
        var result  = await mediator.Send(command);
        return Ok(result);
    }
    
    /// <summary>
    /// Exclui um retiro pelo seu ID.
    /// (Admin,Gestor)
    /// </summary>
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteRetreatCommand(id));
        return NoContent();
    }
    
}
