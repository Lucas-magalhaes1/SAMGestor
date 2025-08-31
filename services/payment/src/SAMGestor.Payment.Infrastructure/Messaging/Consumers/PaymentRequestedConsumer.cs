using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Domain.Entities;
using SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Payment.Infrastructure.Options;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.Infrastructure.Messaging.Consumers;

public sealed class PaymentRequestedConsumer(
    IServiceProvider sp,
    ILogger<PaymentRequestedConsumer> logger,
    RabbitMqOptions mq,
    RabbitMqConnection conn,
    PaymentLinkOptions linkOptions
) : BackgroundService
{
    private const string QueueName = "payment.requested";
    private const string BindKeyV1 = EventTypes.PaymentRequestedV1;

    private static readonly JsonSerializerOptions JsonOpt = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    logger.LogInformation("PaymentRequestedConsumer starting…");

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            // Tenta obter/criar conexão e canal. Se falhar, cai no catch e faz retry.
            var connection = await conn.GetOrCreateAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Setup de infra
            await channel.ExchangeDeclareAsync(mq.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(QueueName, mq.Exchange, BindKeyV1, cancellationToken: stoppingToken);
            await channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

            // Loop de consumo (polling com BasicGet)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
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
                        var env  = JsonSerializer.Deserialize<EventEnvelope<PaymentRequestedV1>>(json, JsonOpt);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope for {RoutingKey}", delivery.RoutingKey);
                            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing PaymentRequested");
                        // MVP: NACK sem requeue para evitar loop infinito (DLQ depois)
                        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
                // Se o canal/conexão fechar, rethrow para o while externo recriar tudo
                catch (Exception ex) when (
                    ex is RabbitMQ.Client.Exceptions.AlreadyClosedException ||
                    ex is RabbitMQ.Client.Exceptions.OperationInterruptedException
                )
                {
                    logger.LogWarning(ex, "Canal/Conexão do RabbitMQ foi fechado. Vou recriar em 5s…");
                    try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (TaskCanceledException) { }
                    throw; // sobe para o while externo
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PaymentRequestedConsumer loop error");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            logger.LogWarning(ex, "RabbitMQ indisponível. Tentando de novo em 5s…");
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (TaskCanceledException) { }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar o consumer. Retry em 5s…");
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (TaskCanceledException) { }
        }
    }

    logger.LogInformation("PaymentRequestedConsumer stopped.");
    }

    private async Task HandleAsync(PaymentRequestedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Idempotência por RegistrationId
        var payment = await db.Payments
            .SingleOrDefaultAsync(p => p.RegistrationId == evt.RegistrationId, ct);

        if (payment is null)
        {
            // seu domínio não tem mais ParticipantId
            payment = new Domain.Entities.Payment(
                registrationId: evt.RegistrationId,
                retreatId:      evt.RetreatId,
                amount:         evt.Amount,
                currency:       evt.Currency
            );
            await db.Payments.AddAsync(payment, ct);
        }

        // Cria link se ainda não tiver (ou republica mesmo link)
        if (string.IsNullOrWhiteSpace(payment.LinkUrl))
        {
            var baseUrl = linkOptions.PublicBaseUrl.TrimEnd('/');
            // opção A: confirmar direto
            var link = $"{baseUrl}/fake/confirm/{payment.Id}?method=pix";

            // SetLink já marca Status = LinkCreated e atualiza UpdatedAt
            payment.SetLink(linkUrl: link, preferenceId: "fake-pref", expiresAt: null);
        }

        // Enfileira o evento (Outbox) e confirma TUDO junto
        var created = new PaymentLinkCreatedV1(
            PaymentId:      payment.Id,
            RegistrationId: payment.RegistrationId,
            RetreatId:      payment.RetreatId,
            Amount:         payment.Amount,
            Currency:       payment.Currency,
            LinkUrl:        payment.LinkUrl!,
            ExpiresAt:      payment.ExpiresAt
        );

        await bus.EnqueueAsync(
            type: EventTypes.PaymentLinkCreatedV1,
            source: "sam.payment",
            data: created,
            ct: ct
        );

        await db.SaveChangesAsync(ct); // Payment + Outbox
    }
}
