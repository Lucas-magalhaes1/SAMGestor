using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Tents.BulkCreate;
using SAMGestor.Application.Features.Tents.Create;
using SAMGestor.Application.Features.Tents.Delete;
using SAMGestor.Application.Features.Tents.GetById;
using SAMGestor.Application.Features.Tents.List;
using SAMGestor.Application.Features.Tents.Locking;
using SAMGestor.Application.Features.Tents.Update;
using SAMGestor.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Retreat;

[ApiController]
[Route("api/retreats/{retreatId:guid}/tents")]
[SwaggerTag("Operações relacionadas às barracas dos retiros.")]
[Authorize(Policy = Policies.ReadOnly)] 
public class RetreatTentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lista barracas do retiro com filtros opcionais.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListTentsResponse>> List(
        [FromRoute] Guid retreatId,
        [FromQuery] string? category = null,
        [FromQuery] bool? onlyActive = null,
        CancellationToken ct = default)
    {
        TentCategory? cat = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<TentCategory>(category, ignoreCase: true, out var parsed))
                return BadRequest(new { error = "Invalid category. Use 'Male' or 'Female'." });
            cat = parsed;
        }

        var res = await mediator.Send(new ListTentsQuery(retreatId, cat, onlyActive), ct);
        return Ok(res);
    }

    /// <summary>
    /// Detalhe de uma barraca.
    /// (Admin,Gestor,Consultor)
    /// </summary>
    [HttpGet("{tentId:guid}")]
    public async Task<ActionResult<GetTentByIdResponse>> GetById(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid tentId,
        CancellationToken ct = default)
    {
        var res = await mediator.Send(new GetTentByIdQuery(retreatId, tentId), ct);
        return Ok(res);
    }

    /// <summary>
    /// Cria uma barraca individual.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid retreatId,
        [FromBody]  CreateTentCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId };
        var res = await mediator.Send(cmd, ct);

        // 201 com Location apontando para GET /tents/{tentId}
        return CreatedAtAction(
            nameof(GetById),
            new { retreatId, tentId = res.TentId },
            res
        );
    }

    /// <summary>
    /// Criação em lote de barracas.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("tents/bulk")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
        public async Task<ActionResult<BulkCreateTentsResponse>> BulkCreate(
            [FromRoute] Guid retreatId,
            [FromBody]  BulkCreateTentsRequest body,
            CancellationToken ct)
        {
            // garante uma lista tipada
            var src = body.Items ?? new List<BulkCreateTentItemRequest>();

            // mapeia request (string Category) -> DTO da Application (enum TentCategory)
            var items = src.Select(i =>
            {
                if (!Enum.TryParse<TentCategory>(i.Category, ignoreCase: true, out var cat))
                {
                    // 400 se categoria inválida
                    throw new ArgumentException(
                        $"Invalid category '{i.Category}' for tent '{i.Number}'. Use 'Male' or 'Female'.");
                }

                // NOTE: seu DTO tem campo 'Notes' (plural). O request tem 'Note'.
                return new BulkCreateTentItemDto(
                    Number:   i.Number,
                    Category: cat,
                    Capacity: i.Capacity,
                    Notes:    i.Note
                );
            }).ToList();

            var cmd = new BulkCreateTentsCommand(
                RetreatId: retreatId,
                Items:     items
            );

            var res = await mediator.Send(cmd, ct);
            return Ok(res);
        }

    /// <summary>
    /// Atualiza dados básicos da barraca (número, categoria, capacidade, observação).
    /// (Admin,Gestor)
    /// </summary>
    [HttpPut("{tentId:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<UpdateTentResponse>> Update(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid tentId,
        [FromBody]  UpdateTentCommand body,
        CancellationToken ct)
    {
        var cmd = body with { RetreatId = retreatId, TentId = tentId };
        var res = await mediator.Send(cmd, ct);
        return Ok(res);
    }

    /// <summary>
    /// Exclui uma barraca (sem remover alocações ainda; bloqueado se estiver locked).
    /// (Admin,Gestor)
    /// </summary>
    [HttpDelete("{tentId:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid tentId,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteTentCommand(retreatId, tentId), ct);
        return NoContent();
    }

    /// <summary>
    /// Lock/Unlock global das barracas do retiro.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<SetTentsGlobalLockResponse>> SetGlobalLock(
        [FromRoute] Guid retreatId,
        [FromBody]  LockRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new SetTentsGlobalLockCommand(retreatId, body.Lock), ct);
        return Ok(res);
    }

    /// <summary>
    /// Lock/Unlock de uma barraca específica.
    /// (Admin,Gestor)
    /// </summary>
    [HttpPost("{tentId:guid}/lock")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<SetTentLockResponse>> SetTentLock(
        [FromRoute] Guid retreatId,
        [FromRoute] Guid tentId,
        [FromBody]  LockRequest body,
        CancellationToken ct)
    {
        var res = await mediator.Send(new SetTentLockCommand(retreatId, tentId, body.Lock), ct);
        return Ok(res);
    }
    public sealed record BulkCreateTentsRequest(List<BulkCreateTentItemRequest>? Items);
    public sealed record BulkCreateTentItemRequest(string Number, string Category, int Capacity, string? Note);
    public sealed record LockRequest(bool Lock);
}
