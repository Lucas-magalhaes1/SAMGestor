using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Messaging.RabbitMq;

namespace SAMGestor.Infrastructure.Messaging.Consumers;

public sealed class PaymentConfirmedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<PaymentConfirmedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "core.payment";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PaymentConfirmedConsumer starting…");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, opt.Exchange, EventTypes.PaymentConfirmedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("PaymentConfirmedConsumer listening on {queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null)
                    {
                        await Task.Delay(400, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env = JsonSerializer.Deserialize<EventEnvelope<PaymentConfirmedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing payment.confirmed.v1. Tag={tag}", delivery.DeliveryTag);
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "PaymentConfirmedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        logger.LogInformation("PaymentConfirmedConsumer stopped.");
    }

    private async Task HandleAsync(PaymentConfirmedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();

        // 1) Atualiza/insere o Payment do CORE (opcional, mas você já tem a entidade)
        //    Se existir, marca como pago. Se não existir, cria e marca como pago.
        var corePayment = await db.Payments
            .SingleOrDefaultAsync(p => p.RegistrationId == evt.RegistrationId, ct);

        var method = NormalizeMethod(evt.Method);
        var money  = new Money(evt.Amount, "BRL");

        if (corePayment is null)
        {
            corePayment = new SAMGestor.Domain.Entities.Payment(
                registrationId: evt.RegistrationId,
                amount: money,
                method: method
            );
            // a entidade já inicia como Pending; vamos marcar como pago:
            corePayment.MarkAsPaid(); // define Status=Paid e PaidAt=UtcNow
            // se quiser respeitar o PaidAt do evento:
            if (evt.PaidAt != default) // set opcional
            {
                // refletir o PaidAt vindo do payment service
                var paidAtField = typeof(SAMGestor.Domain.Entities.Payment)
                    .GetProperty("PaidAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                paidAtField?.SetValue(corePayment, evt.PaidAt.UtcDateTime);
            }
            await db.Payments.AddAsync(corePayment, ct);
        }
        else
        {
            // idempotência: se já está Paid, mantém; senão, marca
            if (corePayment.Status != PaymentStatus.Paid)
            {
                corePayment.MarkAsPaid();
                // refletir PaidAt do evento se quiser
                if (evt.PaidAt != default)
                {
                    var paidAtField = typeof(SAMGestor.Domain.Entities.Payment)
                        .GetProperty("PaidAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    paidAtField?.SetValue(corePayment, evt.PaidAt.UtcDateTime);
                }
            }
        }

        // 2) Atualiza o status da inscrição
        var reg = await db.Registrations.SingleOrDefaultAsync(x => x.Id == evt.RegistrationId, ct);
        if (reg is not null)
        {
            // regra mínima: vai para PaymentConfirmed se vier de Selected/PendingPayment
            // (não rebaixa quem já está Confirmed)
            if (reg.Status != RegistrationStatus.Confirmed)
            {
                reg.MarkConfirmed();
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static PaymentMethod NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return PaymentMethod.Pix;

        var m = method.Trim().ToLowerInvariant();
        return m switch
        {
            "pix" => PaymentMethod.Pix,
            "boleto" or "bankslip" or "bank_slip" => PaymentMethod.BankSlip,
            "card" or "credit" or "credit_card" => PaymentMethod.Card,
            _ => PaymentMethod.Pix
        };
    }
}
