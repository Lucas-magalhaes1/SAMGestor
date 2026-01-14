using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Families.Create;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Features.Families.GetById;
using SAMGestor.Application.Features.Families.Lock;
using SAMGestor.Application.Features.Families.Reset;
using SAMGestor.Application.Features.Families.Unassigned;
using SAMGestor.Application.Features.Families.Update;
using SAMGestor.Application.Features.Families.UpdateGodparents;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Family;

[ApiController]
[Route("api/retreats/{retreatId:guid}")]
[SwaggerTag("Operações relacionadas às famílias de um retiro")]
[Authorize(Policy = Policies.ReadOnly)]  
public class RetreatFamiliesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gera famílias automaticamente com distribuição inteligente e cores únicas.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families/generate")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Gera famílias automaticamente",
        Description = "Cria famílias com distribuição inteligente baseada em algoritmo que evita parentes, sobrenomes repetidos e mesma cidade. " +
                      "Atribui cores únicas automaticamente. Padrinhos/madrinhas devem ser marcados manualmente depois."
    )]
    [SwaggerResponse(200, "Famílias geradas com sucesso", typeof(GenerateFamiliesResponse))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(422, "Não foi possível gerar famílias (falta de cores disponíveis, etc.)")]
    public async Task<ActionResult<GenerateFamiliesResponse>> Generate(
        [FromRoute] Guid retreatId,
        [FromBody] GenerateFamiliesRequest body,
        CancellationToken ct)
    {
        var cmd = new GenerateFamiliesCommand(
            RetreatId: retreatId,
            MembersPerFamily: body.MembersPerFamily,
            ReplaceExisting: body.ReplaceExisting,
            FillExistingFirst: body.FillExistingFirst
        );

        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista todas as famílias do retiro com métricas, alertas e informações completas.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    [HttpGet("families")]
    [SwaggerOperation(
        Summary = "Lista famílias do retiro",
        Description = "Retorna lista completa com cores, percentuais de gênero, padrinhos/madrinhas e alertas."
    )]
    [SwaggerResponse(200, "Lista de famílias", typeof(GetAllFamiliesResponse))]
    public async Task<ActionResult<GetAllFamiliesResponse>> List(
        [FromRoute] Guid retreatId,
        [FromQuery] bool includeAlerts = true,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllFamiliesQuery(retreatId, includeAlerts), ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhes completos de uma família específica.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    [HttpGet("families/{familyId:guid}")]
    [SwaggerOperation(
        Summary = "Obtém detalhes de uma família",
        Description = "Retorna informações completas incluindo cor, membros com email/telefone, padrinhos/madrinhas e alertas."
    )]
    [SwaggerResponse(200, "Detalhes da família", typeof(GetFamilyByIdResponse))]
    [SwaggerResponse(404, "Família não encontrada")]
    public async Task<ActionResult<GetFamilyByIdResponse>> GetById(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromQuery] bool includeAlerts = true,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFamilyByIdQuery(retreatId, familyId, includeAlerts), ct);
        
        if (result.Family is null)
            return NotFound(new { message = "Família não encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova família manualmente com validações completas.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Cria uma família manualmente",
        Description = "Cria família com nome, cor, membros e opcionalmente padrinhos/madrinhas. " +
                      "Valida alertas (sobrenome, cidade, parentes, composição). Use IgnoreWarnings=true para forçar criação."
    )]
    [SwaggerResponse(201, "Família criada", typeof(CreateFamilyResult))]
    [SwaggerResponse(422, "Alertas impedem criação (use IgnoreWarnings=true para forçar)")]
    [SwaggerResponse(400, "Dados inválidos")]
    public async Task<IActionResult> CreateFamily(
        [FromRoute] Guid retreatId,
        [FromBody] CreateFamilyRequest body,
        CancellationToken ct)
    {
        var cmd = new CreateFamilyCommand(
            RetreatId: retreatId,
            Name: body.Name,
            Capacity: body.Capacity,
            ColorName: body.ColorName,
            MemberIds: body.MemberIds ?? Array.Empty<Guid>(),
            PadrinhoIds: body.PadrinhoIds,
            MadrinhaIds: body.MadrinhaIds,
            IgnoreWarnings: body.IgnoreWarnings
        );

        var result = await mediator.Send(cmd, ct);

        if (!result.Created)
        {
            // 422 com os warnings (sem persistir)
            return UnprocessableEntity(new
            {
                created = false,
                version = result.Version,
                warnings = result.Warnings
            });
        }

        // 201 Created
        return CreatedAtAction(
            nameof(GetById),
            routeValues: new { retreatId, familyId = result.FamilyId },
            value: new
            {
                created = true,
                familyId = result.FamilyId,
                version = result.Version,
                warnings = result.Warnings
            });
    }

    /// <summary>
    /// Atualiza famílias (drag-and-drop) com validações completas.
    /// Permite editar nome, cor, capacidade, membros e padrinhos/madrinhas.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPut("families")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Atualiza famílias (drag-and-drop)",
        Description = "Permite editar múltiplas famílias de uma vez. Suporta drag-and-drop de membros entre famílias. " +
                      "Valida locks, cores únicas, nomes únicos e alertas. Use IgnoreWarnings=true para forçar."
    )]
    [SwaggerResponse(200, "Famílias atualizadas", typeof(UpdateFamiliesResponse))]
    [SwaggerResponse(422, "Erros de validação ou alertas impedem atualização")]
    [SwaggerResponse(409, "Conflito de versão (optimistic locking)")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid retreatId,
        [FromBody] UpdateFamiliesCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId };
        var result = await mediator.Send(cmd, ct);

        var hasErrors = result.Errors is not null && result.Errors.Count > 0;
        var hasWarnings = result.Warnings is not null && result.Warnings.Count > 0;
        var didPersist = result.Families is not null && result.Families.Count > 0;

        if (!didPersist && (hasErrors || hasWarnings))
            return UnprocessableEntity(result);

        return Ok(result);
    }

    /// <summary>
    /// Atualiza padrinhos e madrinhas de uma família específica.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPut("families/{familyId:guid}/godparents")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Atualiza padrinhos e madrinhas",
        Description = "Define ou remove padrinhos (0-2) e madrinhas (0-2) de uma família. " +
                      "Remove todos os anteriores e marca os novos. Valida gênero e que sejam membros da família."
    )]
    [SwaggerResponse(200, "Padrinhos/madrinhas atualizados", typeof(UpdateGodparentsResult))]
    [SwaggerResponse(400, "Dados inválidos (máximo 2 de cada, gênero errado, etc.)")]
    [SwaggerResponse(404, "Família não encontrada")]
    public async Task<ActionResult<UpdateGodparentsResult>> UpdateGodparents(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromBody] UpdateGodparentsRequest body,
        CancellationToken ct)
    {
        var cmd = new UpdateGodparentsCommand(
            RetreatId: retreatId,
            FamilyId: familyId,
            PadrinhoIds: body.PadrinhoIds ?? Array.Empty<Guid>(),
            MadrinhaIds: body.MadrinhaIds ?? Array.Empty<Guid>()
        );

        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Trava ou destrava TODAS as famílias do retiro (lock global).
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Trava/destrava todas as famílias",
        Description = "Lock global que impede qualquer edição em todas as famílias do retiro."
    )]
    [SwaggerResponse(200, "Lock atualizado", typeof(LockFamiliesResponse))]
    public async Task<ActionResult<LockFamiliesResponse>> Lock(
        [FromRoute] Guid retreatId,
        [FromBody] LockFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockFamiliesCommand(retreatId, body.Lock), ct);
        return Ok(res);
    }

    /// <summary>
    /// Trava ou destrava uma família específica (lock individual).
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families/{familyId:guid}/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Trava/destrava uma família específica",
        Description = "Lock individual que impede edição apenas desta família."
    )]
    [SwaggerResponse(200, "Lock atualizado", typeof(LockSingleFamilyResponse))]
    [SwaggerResponse(404, "Família não encontrada")]
    public async Task<ActionResult<LockSingleFamilyResponse>> LockFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        [FromBody] LockFamilyRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new LockSingleFamilyCommand(retreatId, familyId, body.Lock), ct);
        return Ok(res);
    }

    /// <summary>
    /// Deleta uma família específica do retiro.
    /// (Admin,Gestor)
    /// </summary>
    [HttpDelete("families/{familyId:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Deleta uma família",
        Description = "Remove a família e todos os seus membros. Não permite deletar se estiver bloqueada ou com grupo ativo."
    )]
    [SwaggerResponse(200, "Família deletada", typeof(DeleteFamilyResponse))]
    [SwaggerResponse(400, "Família bloqueada ou com grupo ativo")]
    [SwaggerResponse(404, "Família não encontrada")]
    public async Task<ActionResult<DeleteFamilyResponse>> DeleteFamily(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid familyId,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteFamilyCommand(retreatId, familyId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista participantes não atribuídos a nenhuma família.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    [HttpGet("families/unassigned")]
    [SwaggerOperation(
        Summary = "Lista participantes sem família",
        Description = "Retorna participantes confirmados/pagos que ainda não foram alocados em nenhuma família. " +
                      "Suporta filtros por gênero, cidade e busca por nome."
    )]
    [SwaggerResponse(200, "Lista de participantes não alocados", typeof(GetUnassignedResponse))]
    public async Task<ActionResult<GetUnassignedResponse>> Unassigned(
        [FromRoute] Guid retreatId,
        [FromQuery] string? gender = null,
        [FromQuery] string? city = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var res = await mediator.Send(new GetUnassignedQuery(retreatId, gender, city, search), ct);
        return Ok(res);
    }

    /// <summary>
    /// Reseta TODAS as famílias do retiro (deleta tudo).
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("families/reset")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    [SwaggerOperation(
        Summary = "Reseta todas as famílias",
        Description = "Deleta todas as famílias e membros do retiro. Use ForceLockedFamilies=true para deletar mesmo famílias bloqueadas. " +
                      "Não permite se houver grupos WhatsApp/Email ativos."
    )]
    [SwaggerResponse(200, "Famílias resetadas", typeof(ResetFamiliesResponse))]
    [SwaggerResponse(400, "Existem grupos ativos ou todas as famílias estão bloqueadas")]
    public async Task<ActionResult<ResetFamiliesResponse>> Reset(
        [FromRoute] Guid retreatId,
        [FromBody] ResetFamiliesRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new ResetFamiliesCommand(retreatId, body.ForceLockedFamilies), ct);
        return Ok(res);
    }

    // ===== DTOs de Request =====

    public sealed record GenerateFamiliesRequest(
        int MembersPerFamily,
        bool ReplaceExisting = true,
        bool FillExistingFirst = false
    );

    public sealed record ResetFamiliesRequest(bool ForceLockedFamilies);
    
    public sealed record LockFamiliesRequest(bool Lock);
    
    public sealed record LockFamilyRequest(bool Lock);
}
