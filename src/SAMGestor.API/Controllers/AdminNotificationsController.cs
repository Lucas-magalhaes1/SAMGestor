using System.Linq;
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
        var regs = await (
            from r in db.Registrations.AsNoTracking()
            join t in db.Retreats.AsNoTracking() on r.RetreatId equals t.Id
            where r.RetreatId == retreatId && r.Status == RegistrationStatus.Selected
            select new
            {
                RegistrationId = r.Id,
                RetreatId      = r.RetreatId,
                Name           = r.Name.Value,
                Email          = r.Email.Value,
                Phone          = r.Phone != null ? r.Phone.ToString() : null,
                Amount         = t.FeeServir.Amount,
                Currency       = t.FeeServir.Currency
            })
            .ToListAsync(ct);

        foreach (var r in regs)
        {
            var evt = new SelectionParticipantSelectedV1(
                RegistrationId: r.RegistrationId,
                RetreatId:      r.RetreatId,
                Amount:         r.Amount,
                Currency:       r.Currency,
                Name:           r.Name,
                Email:          r.Email,
                Phone:          r.Phone
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
        var r = await (
            from x in db.Registrations.AsNoTracking()
            join t in db.Retreats.AsNoTracking() on x.RetreatId equals t.Id
            where x.Id == registrationId
            select new
            {
                RegistrationId = x.Id,
                RetreatId      = x.RetreatId,
                Status         = x.Status,
                Name           = x.Name.Value,
                Email          = x.Email.Value,
                Phone          = x.Phone != null ? x.Phone.ToString() : null,
                Amount         = t.FeeServir.Amount,
                Currency       = t.FeeServir.Currency
            })
            .FirstOrDefaultAsync(ct);

        if (r is null) return NotFound();
        if (r.Status != RegistrationStatus.Selected)
            return BadRequest(new { error = "Registration is not Selected." });

        var evt = new SelectionParticipantSelectedV1(
            RegistrationId: r.RegistrationId,
            RetreatId:      r.RetreatId,
            Amount:         r.Amount,
            Currency:       r.Currency,
            Name:           r.Name,
            Email:          r.Email,
            Phone:          r.Phone
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
