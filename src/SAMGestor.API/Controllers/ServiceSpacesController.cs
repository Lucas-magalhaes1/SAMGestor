using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Dtos;
using SAMGestor.Application.Features.Service.Spaces.PublicList;
using SAMGestor.Application.Features.Service.Spaces.Summary;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/spaces")]
public class ServiceSpacesController(IMediator mediator) : ControllerBase
{
   
    [HttpGet("public")]
    public async Task<ActionResult<IReadOnlyList<ServiceSpacePublicDto>>> GetPublic(Guid retreatId, CancellationToken ct)
    {
        var result = await mediator.Send(new PublicListServiceSpacesQuery(retreatId), ct);
        return Ok(result);
    }
    
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyList<ServiceSpaceSummaryDto>>> GetSummary(Guid retreatId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetServiceSpacesSummaryQuery(retreatId), ct);
        return Ok(result);
    }
}