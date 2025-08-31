using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Domain.Enums;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.API.Controllers;

[ApiController]
[Route("fake")]
public class FakePaymentsController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly IEventBus _bus;

    public FakePaymentsController(PaymentDbContext db, IEventBus bus)
    {
        _db  = db;
        _bus = bus;
    }

    // A) Confirma direto via link (útil para testes rápidos)
    //    Ex.: GET /fake/confirm/{paymentId}?method=pix
    [HttpGet("confirm/{paymentId:guid}")]
    public async Task<IActionResult> Confirm(Guid paymentId, [FromQuery] string? method, CancellationToken ct)
    {
        var html = await ConfirmAndPublishAsync(paymentId, method, ct);
        return Content(html, "text/html");
    }

    // B1) Tela simples de "checkout fake" (GET)
    //     Ex.: GET /fake/checkout/{paymentId}
    [HttpGet("checkout/{paymentId:guid}")]
    public async Task<IActionResult> Checkout(Guid paymentId, CancellationToken ct)
    {
        var p = await _db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId, ct);
        if (p is null) return NotFound("Payment not found.");

        var html = $@"<html><body>
  <h2>Checkout Fake</h2>
  <p>PaymentId: {p.Id}</p>
  <p>Amount: {p.Amount:0.00} {p.Currency}</p>
  <form method=""post"" action=""/fake/checkout/{p.Id}/pay?method=pix"">
    <button type=""submit"">Pagar (PIX)</button>
  </form>
</body></html>";
        return Content(html, "text/html");
    }

    // B2) Ação que "paga" e publica o evento (POST)
    //     Ex.: POST /fake/checkout/{paymentId}/pay?method=pix
    [HttpPost("checkout/{paymentId:guid}/pay")]
    public async Task<IActionResult> CheckoutPay(Guid paymentId, [FromQuery] string? method, CancellationToken ct)
    {
        var html = await ConfirmAndPublishAsync(paymentId, method, ct);
        return Content(html, "text/html");
    }

    // ----------------- helpers -----------------

    private async Task<string> ConfirmAndPublishAsync(Guid paymentId, string? method, CancellationToken ct)
    {
        method ??= "pix";

        var p = await _db.Payments.SingleOrDefaultAsync(x => x.Id == paymentId, ct);
        if (p is null)
            return "<html><body><h2>✖ pagamento não encontrado</h2></body></html>";

        // Idempotência:
        // - Se já está Paid, reemite o evento usando o PaidAt já gravado (ou grava agora se estiver nulo).
        // - Se ainda não estiver Paid, marca como pago agora e salva.
        var paidAt = p.PaidAt ?? DateTimeOffset.UtcNow;
        var providerPaymentId = p.ProviderPaymentId ?? $"fake-{Guid.NewGuid():N}";

        if (p.Status != PaymentStatus.Paid)
        {
            p.MarkPaid(providerPaymentId, paidAt);
            await _db.SaveChangesAsync(ct); // persiste o 'Paid' antes de enfileirar
        }

        // Enfileira o evento na Outbox (será publicado pelo OutboxDispatcher)
        var evt = new PaymentConfirmedV1(
            PaymentId:      p.Id,
            RegistrationId: p.RegistrationId,
            RetreatId:      p.RetreatId,
            Amount:         p.Amount,
            Method:         method,
            PaidAt:         paidAt
        );

        await _bus.EnqueueAsync(
            type:   EventTypes.PaymentConfirmedV1,
            source: "sam.payment",
            data:   evt,
            ct:     ct
        );

        await _db.SaveChangesAsync(ct); // grava outbox

        return $@"<html><body>
  <h2>✔ pagamento confirmado</h2>
  <p>PaymentId: {p.Id}</p>
  <p>Status: Paid</p>
</body></html>";
    }
}
