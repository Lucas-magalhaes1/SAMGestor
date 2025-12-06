using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Service.Roster.Update;
using SAMGestor.Application.Features.Service.Spaces.BulkCapacity;
using SAMGestor.Application.Features.Service.Spaces.Create;
using SAMGestor.Application.Features.Service.Spaces.Delete;
using SAMGestor.Application.Features.Service.Spaces.Detail;
using SAMGestor.Application.Features.Service.Spaces.List;
using SAMGestor.Application.Features.Service.Spaces.Locking;
using SAMGestor.Application.Features.Service.Spaces.PublicList;
using SAMGestor.Application.Features.Service.Spaces.Summary;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Service;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/spaces")]
[SwaggerTag("Operações relacionadas às áreas de serviço.")]
[Authorize(Policy = Policies.ReadOnly)]
public class ServiceSpacesController(IMediator mediator) : ControllerBase
{
    /// <summary> Lista de áreas de serviço públicas (sem retiro).(Admin,Gestor,Consultor) </summary>
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new PublicListServiceSpacesQuery(retreatId), ct));
    
    /// <summary> Resumo das áreas de serviço.(Admin,Gestor,Consultor) </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(Guid retreatId, CancellationToken ct)
        => Ok(await mediator.Send(new GetServiceSpacesSummaryQuery(retreatId), ct));

    /// <summary> Lista de áreas de serviço.(Admin,Gestor,Consultor) </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        Guid retreatId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isLocked,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = new ListServiceSpacesQuery(
            RetreatId: retreatId,
            IsActive: isActive,
            IsLocked: isLocked,
            Search: search
        );

        var res = await mediator.Send(query, ct);
        return Ok(res);
    }

    
    /// <summary> Detalhe de uma área de serviço.(Admin,Gestor,Consultor) </summary>
    [HttpGet("{spaceId:guid}")]
    public async Task<IActionResult> Detail(
        Guid retreatId,
        Guid spaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetServiceSpaceDetailQuery(retreatId, spaceId, page, pageSize, q), ct));
    
    /// <summary> Cria uma nova área de serviço. (Admin,Gestor)</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
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
    
    /// <summary> Exclui uma área de serviço.(Admin,Gestor) </summary>
    [HttpDelete("{spaceId:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> Delete(Guid retreatId, Guid spaceId, CancellationToken ct)
    {
        await mediator.Send(new DeleteServiceSpaceCommand(retreatId, spaceId), ct);
        return NoContent();
    }
    
    
    /// <summary> Atualiza a capacidade de uma ou mais áreas de serviço.(Admin,Gestor) </summary>
    [HttpPost("capacity")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
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
    
    
    /// <summary> Lock/Unlock de uma área de serviço. (Admin,Gestor)</summary>

    [HttpPost("{spaceId:guid}/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> ToggleLock(Guid retreatId, Guid spaceId, [FromBody] ToggleServiceSpaceLockRequest body, CancellationToken ct)
    {
        await mediator.Send(new LockServiceSpaceCommand(retreatId, spaceId, body.Lock), ct);
        return NoContent();
    }
    
    /// <summary> Lock/Unlock global das áreas de serviço.(Admin,Gestor) </summary>
    [HttpPost("lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> ToggleLockAll(Guid retreatId, [FromBody] ToggleServiceSpaceLockRequest body, CancellationToken ct)
    {
        await mediator.Send(new LockAllServiceSpacesCommand(retreatId, body.Lock), ct);
        return NoContent();
    }
    
    /// <summary> Atualiza o quadro de serviço (roster). (Admin,Gestor)</summary>
    [HttpPut("~/api/retreats/{retreatId:guid}/service/roster")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> UpdateRoster(Guid retreatId, [FromBody] UpdateServiceRosterCommand body, CancellationToken ct)
        => Ok(await mediator.Send(body with { RetreatId = retreatId }, ct));
}
