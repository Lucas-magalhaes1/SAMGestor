using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Spaces.PublicList;
using SAMGestor.Application.Features.Service.Spaces.Summary;
using SAMGestor.Application.Features.Service.Spaces.Detail;
using SAMGestor.Application.Features.Service.Spaces.Create;
using SAMGestor.Application.Features.Service.Spaces.Delete;
using SAMGestor.Application.Features.Service.Spaces.BulkCapacity;
using SAMGestor.Application.Features.Service.Spaces.Locking;
using SAMGestor.Application.Features.Service.Roster.Update;
using SAMGestor.Application.Features.Service.Spaces.List;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/spaces")]
public class ServiceSpacesController(IMediator mediator) : ControllerBase
{
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new PublicListServiceSpacesQuery(retreatId), ct));
    
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetServiceSpacesSummaryQuery(retreatId), ct));

    [HttpGet]
    public async Task<IActionResult> GetList(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new ListServiceSpacesQuery(retreatId), ct));
    
    [HttpGet("{spaceId:guid}")]
    public async Task<IActionResult> Detail(
        Guid retreatId,
        Guid spaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetServiceSpaceDetailQuery(retreatId, spaceId, page, pageSize, q), ct));
    
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid retreatId,
        [FromBody] CreateServiceSpaceRequest req,
        CancellationToken ct)
    {
        var res = await mediator.Send(new CreateServiceSpaceCommand(
            RetreatId: retreatId,
            Name: req.Name,
            Description: req.Description,
            MinPeople: req.MinPeople,
            MaxPeople: req.MaxPeople,
            IsActive: req.IsActive
        ), ct);

        return Ok(res);
    }
    
    [HttpDelete("{spaceId:guid}")]
    public async Task<IActionResult> Delete(Guid retreatId, Guid spaceId, CancellationToken ct)
    {
        await mediator.Send(new DeleteServiceSpaceCommand(retreatId, spaceId), ct);
        return NoContent();
    }
    
    
    [HttpPost("capacity")]
    public async Task<IActionResult> BulkCapacity(Guid retreatId, [FromBody] UpdateServiceSpacesCapacityRequest req, CancellationToken ct)
    {
        
        IReadOnlyList<UpdateServiceSpacesCapacityCommand.Item>? items = null;
        if (req.Items is not null)
            items = req.Items.Select(i => new UpdateServiceSpacesCapacityCommand.Item(i.SpaceId, i.MinPeople, i.MaxPeople)).ToList();

        var res = await mediator.Send(new UpdateServiceSpacesCapacityCommand(
            RetreatId: retreatId,
            ApplyToAll: req.ApplyToAll,
            MinPeople: req.MinPeople,
            MaxPeople: req.MaxPeople,
            Items: items
        ), ct);

        return Ok(res);
    }
    
    

    [HttpPost("{spaceId:guid}/lock")]
    public async Task<IActionResult> ToggleLock(Guid retreatId, Guid spaceId, [FromBody] ToggleServiceSpaceLockRequest body, CancellationToken ct)
    {
        await mediator.Send(new LockServiceSpaceCommand(retreatId, spaceId, body.Lock), ct);
        return NoContent();
    }
    
    [HttpPost("lock")]
    public async Task<IActionResult> ToggleLockAll(Guid retreatId, [FromBody] ToggleServiceSpaceLockRequest body, CancellationToken ct)
    {
        await mediator.Send(new LockAllServiceSpacesCommand(retreatId, body.Lock), ct);
        return NoContent();
    }
    
    [HttpPut("~/api/retreats/{retreatId:guid}/service/roster")]
    public async Task<IActionResult> UpdateRoster(Guid retreatId, [FromBody] UpdateServiceRosterCommand body, CancellationToken ct)
        => Ok(await mediator.Send(body with { RetreatId = retreatId }, ct));
}
