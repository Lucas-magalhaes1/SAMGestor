using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Domain.Enums;
using SAMGestor.Payment.Infrastructure.Options;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.API.Controllers;

[ApiController]
[Route("api/mercadopago")]
public sealed class MercadoPagoWebhookController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly IEventBus _bus;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoWebhookController> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MercadoPagoWebhookController(
        PaymentDbContext db,
        IEventBus bus,
        IHttpClientFactory httpClientFactory,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoWebhookController> logger)
    {
        _db = db;
        _bus = bus;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var query = HttpContext.Request.Query;

        // 1) Formato novo: ?type=payment&data.id=123456
        if (query.TryGetValue("type", out var typeVals) &&
            string.Equals(typeVals.ToString(), "payment", StringComparison.OrdinalIgnoreCase) &&
            query.TryGetValue("data.id", out var dataIdVals))
        {
            var paymentIdStr = dataIdVals.ToString();
            _logger.LogInformation("Webhook: type=payment, data.id={Id}", paymentIdStr);

            await HandlePaymentNotificationAsync(paymentIdStr, ct);
            return Ok();
        }

        // 2) Formato legacy: ?topic=payment&id=123456
        if (query.TryGetValue("topic", out var topicVals) &&
            string.Equals(topicVals.ToString(), "payment", StringComparison.OrdinalIgnoreCase) &&
            query.TryGetValue("id", out var paymentIdVals))
        {
            var paymentIdStr = paymentIdVals.ToString();
            _logger.LogInformation("Webhook: topic=payment, id={Id}", paymentIdStr);

            await HandlePaymentNotificationAsync(paymentIdStr, ct);
            return Ok();
        }

        // 3) Formato merchant_order: ?topic=merchant_order&id=123456
        if (query.TryGetValue("topic", out topicVals) &&
            string.Equals(topicVals.ToString(), "merchant_order", StringComparison.OrdinalIgnoreCase) &&
            query.TryGetValue("id", out var moIdVals))
        {
            var merchantOrderIdStr = moIdVals.ToString();
            _logger.LogInformation("Webhook: topic=merchant_order, id={Id}", merchantOrderIdStr);

            await HandleMerchantOrderNotificationAsync(merchantOrderIdStr, ct);
            return Ok();
        }

        _logger.LogInformation("Webhook ignorado. QueryString={Query}", Request.QueryString.Value);
        return Ok();
    }

    // ---------------- helpers ----------------

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("mercadopago");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return client;
    }

    /// <summary>
    /// type=payment&data.id=XYZ  OU  topic=payment&id=XYZ
    /// → GET /v1/payments/{id}
    /// </summary>
    private async Task HandlePaymentNotificationAsync(string paymentIdStr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paymentIdStr))
        {
            _logger.LogWarning("paymentId vazio no webhook (payment).");
            return;
        }

        using var client = CreateClient();

        var url = $"https://api.mercadopago.com/v1/payments/{paymentIdStr}";
        using var resp = await client.GetAsync(url, ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Erro ao consultar payment {PaymentId}: {Status} {Reason}",
                paymentIdStr, (int)resp.StatusCode, resp.ReasonPhrase);
            return;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = json.RootElement;

        var status = root.TryGetProperty("status", out var stEl)
            ? stEl.GetString()
            : null;

        if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Payment {PaymentId} com status {Status}, nada a fazer.", paymentIdStr, status);
            return;
        }

        var extRef = root.TryGetProperty("external_reference", out var extEl)
            ? extEl.GetString()
            : null;

        if (!Guid.TryParse(extRef, out var paymentId))
        {
            _logger.LogWarning("external_reference inválido em payment {PaymentId}: {ExtRef}",
                paymentIdStr, extRef);
            return;
        }

        // Método de pagamento
        string methodId =
            root.TryGetProperty("payment_method_id", out var pmEl) &&
            pmEl.ValueKind == JsonValueKind.String
                ? (pmEl.GetString() ?? "mercadopago")
                : "mercadopago";

        // Data de aprovação
        DateTimeOffset paidAt = DateTimeOffset.UtcNow;
        if (root.TryGetProperty("date_approved", out var dtEl) &&
            dtEl.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(dtEl.GetString(), out var parsed))
        {
            paidAt = parsed;
        }

        await MarkPaymentAsPaidAndPublishAsync(paymentId, paymentIdStr, methodId, paidAt, ct);
    }

    /// <summary>
    /// topic=merchant_order&id=XYZ  → GET /merchant_orders/{id}
    /// </summary>
    private async Task HandleMerchantOrderNotificationAsync(string merchantOrderIdStr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(merchantOrderIdStr))
        {
            _logger.LogWarning("merchant_order id vazio no webhook.");
            return;
        }

        using var client = CreateClient();

        var url = $"https://api.mercadopago.com/merchant_orders/{merchantOrderIdStr}";
        using var resp = await client.GetAsync(url, ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Erro ao consultar merchant_order {Id}: {Status} {Reason}",
                merchantOrderIdStr, (int)resp.StatusCode, resp.ReasonPhrase);
            return;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = json.RootElement;

        var extRef = root.TryGetProperty("external_reference", out var extEl)
            ? extEl.GetString()
            : null;

        if (!Guid.TryParse(extRef, out var paymentId))
        {
            _logger.LogWarning("external_reference inválido em merchant_order {Id}: {ExtRef}",
                merchantOrderIdStr, extRef);
            return;
        }

        if (!root.TryGetProperty("payments", out var paymentsEl) ||
            paymentsEl.ValueKind != JsonValueKind.Array)
        {
            _logger.LogInformation("merchant_order {Id} sem pagamentos ainda.", merchantOrderIdStr);
            return;
        }

        JsonElement? approvedPayment = null;

        foreach (var el in paymentsEl.EnumerateArray())
        {
            if (el.TryGetProperty("status", out var stEl) &&
                string.Equals(stEl.GetString(), "approved", StringComparison.OrdinalIgnoreCase))
            {
                approvedPayment = el;
                break;
            }
        }

        if (approvedPayment is null)
        {
            _logger.LogInformation("merchant_order {Id} sem pagamentos aprovados ainda.", merchantOrderIdStr);
            return;
        }

        var ap = approvedPayment.Value;

        var providerPaymentId = ap.TryGetProperty("id", out var idEl)
            ? idEl.GetInt64().ToString()
            : merchantOrderIdStr;

        string methodId =
            ap.TryGetProperty("payment_method_id", out var pmEl) &&
            pmEl.ValueKind == JsonValueKind.String
                ? (pmEl.GetString() ?? "mercadopago")
                : "mercadopago";

        DateTimeOffset paidAt = DateTimeOffset.UtcNow;
        if (ap.TryGetProperty("date_approved", out var dtEl) &&
            dtEl.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(dtEl.GetString(), out var parsed))
        {
            paidAt = parsed;
        }

        await MarkPaymentAsPaidAndPublishAsync(paymentId, providerPaymentId, methodId, paidAt, ct);
    }

    private async Task MarkPaymentAsPaidAndPublishAsync(
        Guid paymentId,
        string providerPaymentId,
        string methodId,
        DateTimeOffset paidAt,
        CancellationToken ct)
    {
        var p = await _db.Payments.SingleOrDefaultAsync(x => x.Id == paymentId, ct);
        if (p is null)
        {
            _logger.LogWarning("Payment não encontrado para Id={PaymentId}", paymentId);
            return;
        }

        // Sempre salvar em UTC por causa do PostgreSQL (timestamptz + DateTimeOffset)
        var utcPaidAt = paidAt.ToUniversalTime();

        var effectivePaidAt = p.PaidAt ?? utcPaidAt;
        var effectiveProviderPaymentId = p.ProviderPaymentId ?? providerPaymentId;

        if (p.Status != PaymentStatus.Paid)
        {
            p.MarkPaid(effectiveProviderPaymentId, effectivePaidAt);
            await _db.SaveChangesAsync(ct);
        }

        // Garantia extra contra null (e some o warning do Method)
        methodId ??= "mercadopago";

        var evt = new PaymentConfirmedV1(
            PaymentId:      p.Id,
            RegistrationId: p.RegistrationId,
            RetreatId:      p.RetreatId,
            Amount:         p.Amount,
            Method:         methodId,
            PaidAt:         effectivePaidAt
        );

        await _bus.EnqueueAsync(
            type:   EventTypes.PaymentConfirmedV1,
            source: "sam.payment",
            data:   evt,
            ct:     ct
        );

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment {PaymentId} marcado como Paid (UTC {PaidAt}) e PaymentConfirmedV1 publicado.",
            p.Id, effectivePaidAt);
    }
}
