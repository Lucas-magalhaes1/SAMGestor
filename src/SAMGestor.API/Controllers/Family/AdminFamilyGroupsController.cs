using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Families.Groups.Create;
using SAMGestor.Application.Features.Families.Groups.Notify;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Family;

[ApiController]
[Route("admin/retreats/{retreatId:guid}/groups")]
[SwaggerTag("Operações relacionadas às famílias.")]
public class AdminFamilyGroupsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Cria/Notifica grupos para TODAS as famílias do retiro (bulk).
    /// Requer lock global do retiro.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateFamilyGroupsResponse>> CreateAllGroups(
        [FromRoute] Guid retreatId,
        [FromBody]  CreateFamilyGroupsCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId };
        var res = await mediator.Send(cmd, ct);
        return Accepted(res); // 202
    }

    /// <summary>
    /// Cria/Notifica o grupo de UMA família específica.
    /// Requer lock global OU lock da família.
    /// </summary>
    [HttpPost("{familyId:guid}/notify")]
    public async Task<ActionResult<NotifyFamilyGroupResponse>> NotifyFamilyGroup(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromBody]  NotifyFamilyGroupCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId, FamilyId = familyId };
        var res = await mediator.Send(cmd, ct);
        return Accepted(res); // 202
    }
}