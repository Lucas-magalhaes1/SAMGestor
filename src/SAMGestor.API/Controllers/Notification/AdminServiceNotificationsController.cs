using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.API.Auth;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Enums;
using SAMGestor.Infrastructure.Persistence;
using Swashbuckle.AspNetCore.Annotations;


namespace SAMGestor.API.Controllers.Notification;

[ApiController]
[Route("admin/notifications/service")]
[SwaggerTag("Operações relacionadas às notificações de inscrições para servir em retiros. (Admin,Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public sealed class AdminServiceNotificationsController(
    SAMContext db,
    IEventBus bus
) : ControllerBase
{
    // 1) Em massa: todos os inscritos do retiro (Servir)
    //    Padrão: usa FeeServir e marca Notified após enfileirar
    //    Filtro mínimo para evitar “spam” em confirmados/declinados/cancelados:
    //    Status IN (Submitted, Notified) e Enabled = true.
    
    /// <summary>
    ///  Notifica todos os participantes selecionados para servir em um retiro específico.
    /// </summary>
    
    
    [HttpPost("retreats/{retreatId:guid}/notify-selected")]
    public async Task<IActionResult> NotifySelectedForRetreat(Guid retreatId, CancellationToken ct)
    {
        // Traz somente o necessário para montar o evento (join para pegar FeeServir)
        var regs = await (
            from r in db.ServiceRegistrations.AsNoTracking()
            join t in db.Retreats.AsNoTracking() on r.RetreatId equals t.Id
            where r.RetreatId == retreatId
               && r.Enabled
               && (r.Status == ServiceRegistrationStatus.Submitted || r.Status == ServiceRegistrationStatus.Notified)
            select new
            {
                RegistrationId = r.Id,
                RetreatId      = r.RetreatId,
                Name           = r.Name.Value,
                Email          = r.Email.Value,
                Phone          = r.Phone,
                Amount         = t.FeeServir.Amount,
                Currency       = t.FeeServir.Currency
            })
            .ToListAsync(ct);

        // Publica 1 evento por inscrição
        foreach (var r in regs)
        {
            var evt = new ServingParticipantSelectedV1(
                RegistrationId: r.RegistrationId,
                RetreatId:      r.RetreatId,
                Amount:         r.Amount,
                Currency:       r.Currency,
                Name:           r.Name,
                Email:          r.Email,
                Phone:          r.Phone
            );

            await bus.EnqueueAsync(
                type: EventTypes.ServingParticipantSelectedV1,
                source: "sam.core",
                data: evt,
                ct: ct
            );
        }

        // Marca Notified nos registros (idempotente)
        var ids = regs.Select(x => x.RegistrationId).ToList();
        if (ids.Count > 0)
        {
            var toUpdate = await db.ServiceRegistrations
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(ct);

            foreach (var r in toUpdate)
            {
                // Só transita se estiver em Submitted/Notified
                if (r.Status is ServiceRegistrationStatus.Submitted or ServiceRegistrationStatus.Notified)
                    r.MarkNotified();
            }
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { retreatId, count = regs.Count });
    }

    /// <summary>
    ///  Notifica um participante selecionado específico para servir em retiro.
    ///  </summary>
    
    // 2) Individual
    [HttpPost("registrations/{serviceRegistrationId:guid}/notify")]
    public async Task<IActionResult> NotifyOne(Guid serviceRegistrationId, CancellationToken ct)
    {
        var r = await (
            from x in db.ServiceRegistrations.AsNoTracking()
            join t in db.Retreats.AsNoTracking() on x.RetreatId equals t.Id
            where x.Id == serviceRegistrationId
            select new
            {
                RegistrationId = x.Id,
                RetreatId      = x.RetreatId,
                Status         = x.Status,
                Name           = x.Name.Value,
                Email          = x.Email.Value,
                Phone          = x.Phone,
                Amount         = t.FeeServir.Amount,
                Currency       = t.FeeServir.Currency
            })
            .FirstOrDefaultAsync(ct);

        if (r is null) return NotFound();

        // (Opcional) bloquear se já estiver Confirmed/Declined/Cancelled
        if (r.Status is ServiceRegistrationStatus.Confirmed
                     or ServiceRegistrationStatus.Declined
                     or ServiceRegistrationStatus.Cancelled)
        {
            return BadRequest(new { error = "Registration is not eligible for notification." });
        }

        var evt = new ServingParticipantSelectedV1(
            RegistrationId: r.RegistrationId,
            RetreatId:      r.RetreatId,
            Amount:         r.Amount,
            Currency:       r.Currency,
            Name:           r.Name,
            Email:          r.Email,
            Phone:          r.Phone
        );

        await bus.EnqueueAsync(
            type: EventTypes.ServingParticipantSelectedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // Marca Notified (idempotente)
        var tracked = await db.ServiceRegistrations
            .FirstOrDefaultAsync(x => x.Id == serviceRegistrationId, ct);

        if (tracked is not null &&
            tracked.Status is ServiceRegistrationStatus.Submitted or ServiceRegistrationStatus.Notified)
        {
            tracked.MarkNotified();
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { status = "queued", registrationId = serviceRegistrationId });
    }
}
