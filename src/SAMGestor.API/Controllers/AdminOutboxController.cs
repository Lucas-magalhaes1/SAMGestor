using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("admin/outbox")]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly SAMContext _db;
    public AdminOutboxController(SAMContext db) => _db = db;

    
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

    
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? processed,
        [FromQuery] string? type,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var q = _db.OutboxMessages.AsNoTracking().OrderByDescending(x => x.CreatedAt).AsQueryable();

        if (processed is true)  q = q.Where(x => x.ProcessedAt != null);
        if (processed is false) q = q.Where(x => x.ProcessedAt == null);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.Type == type);

        var items = await q
            .Take(Math.Clamp(limit, 1, 500))
            .Select(x => new OutboxMessageDto(x.Id, x.Type, x.CreatedAt, x.ProcessedAt, x.Attempts, x.LastError))
            .ToListAsync(ct);

        return Ok(items);
    }

    
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
