using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.API.Controllers;

[ApiController]
[Route("dev")]
public sealed class DevPaymentsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpt = new(JsonSerializerDefaults.Web);
    
    public sealed record DevPaymentRequest(
        Guid RegistrationId,
        Guid RetreatId,
        decimal Amount,
        string Currency,
        string Name,
        string Email,
        string? Phone
    );

    // POST /dev/emit/payment-request
    [HttpPost("emit/payment-request")]
    public async Task<IActionResult> EmitPaymentRequest(
        [FromBody] DevPaymentRequest body,
        [FromServices] EventPublisher publisher,
        CancellationToken ct)
    {
        var env = EventEnvelope<PaymentRequestedV1>.Create(
            type:   EventTypes.PaymentRequestedV1,
            source: "sam.dev",
            data:   new PaymentRequestedV1(
                        RegistrationId: body.RegistrationId,
                        RetreatId:      body.RetreatId,
                        Amount:         body.Amount,
                        Currency:       body.Currency,
                        Name:           body.Name,
                        Email:          body.Email,
                        Phone:          body.Phone
                    )
        );

        var json = JsonSerializer.Serialize(env, JsonOpt);
        await publisher.PublishAsync(EventTypes.PaymentRequestedV1, json, ct);
        return Accepted(new { status = "published", env.Id });
    }

    // GET /dev/payments/by-registration/{registrationId}
    [HttpGet("payments/by-registration/{registrationId:guid}")]
    public async Task<IActionResult> GetByRegistration(
        Guid registrationId,
        [FromServices] PaymentDbContext db,
        CancellationToken ct)
    {
        var p = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RegistrationId == registrationId, ct);

        if (p is null) return NotFound();

        return Ok(new {
            p.Id, p.RegistrationId, p.RetreatId, p.Amount, p.Currency,
            p.Status, p.LinkUrl, p.Provider, p.ProviderPreferenceId, p.ProviderPaymentId
        });
    }

    // GET /fake/confirm/{paymentId}?method=pix
    // rota ABSOLUTA (começa com '/') para bater exatamente em /fake/confirm/...
    [HttpGet("/fake/confirm/{paymentId:guid}")]
    public async Task<IActionResult> FakeConfirm(
        Guid paymentId,
        [FromQuery] string? method,
        [FromServices] PaymentDbContext db,
        [FromServices] IEventBus bus,
        CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return NotFound("payment not found");

        if (payment.Status != SAMGestor.Payment.Domain.Enums.PaymentStatus.Paid)
        {
            payment.MarkPaid(providerPaymentId: $"fake-{Guid.NewGuid():N}", paidAt: DateTimeOffset.UtcNow);

            var evt = new PaymentConfirmedV1(
                PaymentId:      payment.Id,
                RegistrationId: payment.RegistrationId,
                RetreatId:      payment.RetreatId,
                Amount:         payment.Amount,
                Method:         method ?? "fake",
                PaidAt:         payment.PaidAt ?? DateTimeOffset.UtcNow
            );

            await bus.EnqueueAsync(EventTypes.PaymentConfirmedV1, "sam.payment", evt, ct: ct);
            await db.SaveChangesAsync(ct);
        }

        var html = $"""
        <html><body>
          <h2>✔ pagamento confirmado</h2>
          <p>PaymentId: {payment.Id}</p>
          <p>Status: {payment.Status}</p>
        </body></html>
        """;
        return Content(html, "text/html");
    }
}
