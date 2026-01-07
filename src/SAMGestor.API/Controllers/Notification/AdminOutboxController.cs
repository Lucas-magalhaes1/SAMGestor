using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.API.Auth;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Infrastructure.Persistence;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Notification;

[ApiController]
[Route("admin/outbox")]
[SwaggerTag("Operações relacionadas à fila de mensagens de saída (outbox) para notificações. (Admin,Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly SAMContext _db;
    public AdminOutboxController(SAMContext db) => _db = db;

    /// <summary>
    /// Obtém um resumo do estado atual da fila de mensagens de saída (outbox).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var pending = await _db.OutboxMessages.CountAsync(x => x.ProcessedAt == null, ct);
        var withErrors = await _db.OutboxMessages.CountAsync(x => x.LastError != null && x.ProcessedAt == null, ct);

        var lastProcessed = await _db.OutboxMessages
            .Where(x => x.ProcessedAt != null)
            .OrderByDescending(x => x.ProcessedAt)
            .Select(x => x.ProcessedAt)
            .FirstOrDefaultAsync(ct);

        var oldestPending = await _db.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return Ok(new { pending, withErrors, lastProcessed, oldestPending });
    }

    /// <summary>
    /// Obtém uma lista de mensagens na fila de saída (outbox) com filtros opcionais.
    /// </summary>  
    [HttpGet]
    public async Task<ActionResult<PagedResult<OutboxMessageDto>>> List(
        [FromQuery] bool? processed,
        [FromQuery] string? type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var query = _db.OutboxMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        if (processed is true)  query = query.Where(x => x.ProcessedAt != null);
        if (processed is false) query = query.Where(x => x.ProcessedAt == null);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .ApplyPagination(skip, take)
            .Select(x => new OutboxMessageDto(
                x.Id, 
                x.Type, 
                x.CreatedAt, 
                x.ProcessedAt, 
                x.Attempts, 
                x.LastError))
            .ToListAsync(ct); // ✅ ToListAsync<OutboxMessageDto>

        return Ok(new PagedResult<OutboxMessageDto>(items, totalCount, skip, take));
    }

    /// <summary>
    /// Obtém os detalhes de uma mensagem específica na fila de saída (outbox) pelo seu ID.
    /// </summary>  
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var x = await _db.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (x is null) return NotFound();
        return Ok(new
        {
            x.Id, x.Type, x.Source, x.TraceId, x.CreatedAt, x.ProcessedAt, x.Attempts, x.LastError,
            x.Data 
        });
    }

    /// <summary>
    /// Reenfileira uma mensagem específica na fila de saída (outbox) para reprocessamento.
    /// </summary>  
    [HttpPost("{id:guid}/requeue")]
    public async Task<IActionResult> Requeue(Guid id, CancellationToken ct)
    {
        var m = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();

        m.ProcessedAt = null;
        m.LastError = null;

        await _db.SaveChangesAsync(ct);
        return Ok(new { status = "requeued", id });
    }
}
