using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Enums;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("admin/notifications")]
public class AdminNotificationsController(SAMContext db, IEventBus bus) : ControllerBase
{
    [HttpPost("retreats/{retreatId:guid}/notify-selected")]
    public async Task<IActionResult> NotifySelectedForRetreat(Guid retreatId, CancellationToken ct)
    {
        var regs = await db.Registrations
            .AsNoTracking()
            .Where(r => r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected)
            .Select(r => new
            {
                RegistrationId = r.Id,
                RetreatId      = r.RetreatId,
                Name           = r.Name.Value,
                Email          = r.Email.Value,
                Phone          = r.Phone != null ? r.Phone.ToString() : null
            })
            .ToListAsync(ct);

        foreach (var r in regs)
        {
            var evt = new SelectionParticipantSelectedV1(
                RegistrationId: r.RegistrationId,
                ParticipantId:  r.RegistrationId,  
                Name:           r.Name,
                Email:          r.Email,
                Phone:          r.Phone,
                RetreatId:      r.RetreatId
            );

            await bus.EnqueueAsync(
                type: EventTypes.SelectionParticipantSelectedV1,
                source: "sam.core",
                data: evt,
                ct: ct
            );
        }
        await db.SaveChangesAsync(ct);

        return Ok(new { retreatId, count = regs.Count });
    }
    
    [HttpPost("registrations/{registrationId:guid}/notify")]
    public async Task<IActionResult> NotifyOne(Guid registrationId, CancellationToken ct)
    {
        var r = await db.Registrations
            .AsNoTracking()
            .Where(x => x.Id == registrationId)
            .Select(x => new
            {
                RegistrationId = x.Id,
                RetreatId      = x.RetreatId,
                Status         = x.Status,
                Name           = x.Name.Value,
                Email          = x.Email.Value,
                Phone          = x.Phone != null ? x.Phone.ToString() : null
            })
            .FirstOrDefaultAsync(ct);

        if (r is null) return NotFound();

        if (r.Status != RegistrationStatus.Selected)
            return BadRequest(new { error = "Registration is not Selected." });

        var evt = new SelectionParticipantSelectedV1(
            RegistrationId: r.RegistrationId,
            ParticipantId:  r.RegistrationId, 
            Name:           r.Name,
            Email:          r.Email,
            Phone:          r.Phone,
            RetreatId:      r.RetreatId
        );

        await bus.EnqueueAsync(
            type: EventTypes.SelectionParticipantSelectedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        await db.SaveChangesAsync(ct);

        return Ok(new { status = "queued", registrationId });
    }
}
