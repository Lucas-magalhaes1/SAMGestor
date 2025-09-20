using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;
using SAMGestor.Notification.Infrastructure.Messaging;
using SAMGestor.Notification.Infrastructure.Persistence;

namespace SAMGestor.Notification.Infrastructure.Messaging.Consumers;

public sealed class FamilyGroupCreateRequestedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<FamilyGroupCreateRequestedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.familygroups";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FamilyGroupCreateRequestedConsumer starting…");
        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var ch = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

                // 👇 assinatura correta (tem que passar noWait:false)
                await ch.QueueDeclareAsync(
                    QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: stoppingToken
                );

                await ch.QueueBindAsync(QueueName, exchange, EventTypes.FamilyGroupCreateRequestedV1, cancellationToken: stoppingToken);
                await ch.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("FamilyGroupCreateRequestedConsumer listening on {queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await ch.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null) { await Task.Delay(400, stoppingToken); continue; }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<FamilyGroupCreateRequestedV1>>(json, Json);
                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await ch.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await ch.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing family.group.create.requested.v1");
                        await ch.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
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
                logger.LogError(ex, "FamilyGroupCreateRequestedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(FamilyGroupCreateRequestedV1 req, CancellationToken ct)
    {
        using var scope     = sp.CreateScope();
        var db              = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var repo            = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer        = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels        = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher       = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var emailChannel    = channels.FirstOrDefault(c => c.Name == "email");
        var whatsappChannel = channels.FirstOrDefault(c => c.Name == "whatsapp"); // só se você já tiver implementado

        // === HOJE (stub): "cria" um link fake. Amanhã: chamar Z-API e pegar inviteLink + externalId ===
        var inviteLink = $"https://groups.example/{req.FamilyId}/{Guid.NewGuid():N}";
        string? externalId = null;

        // === Templates simples (pode trocar por seus templates/renderer reais) ===
        var subjectTpl = "Grupo da sua família no retiro";
        var bodyTpl = """
            Olá {{Name}},

            Este é o link do grupo da sua família: {{GroupLink}}

            Qualquer dúvida, fale com a coordenação.
            """;

        // destinatários (e-mail) — distintos
        var emails = req.Members
            .Select(m => m.Email)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 1) Envia por e-mail (Mailhog em dev)
        if (emailChannel is not null && emails.Count > 0)
        {
            foreach (var m in req.Members.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
            {
                var data = new Dictionary<string, string>
                {
                    ["Name"] = m.Name,
                    ["GroupLink"] = inviteLink
                };

                var subject = renderer.Render(subjectTpl, data);
                var body    = renderer.Render(bodyTpl, data);

                var message = new NotificationMessage(
                    channel: NotificationChannel.Email,
                    recipientName: m.Name,
                    recipientEmail: m.Email!,
                    recipientPhone: m.PhoneE164,
                    templateKey: "family-group-link",
                    subject: subject,
                    body: body,
                    registrationId: m.RegistrationId,
                    retreatId: req.RetreatId,
                    externalCorrelationId: req.FamilyId.ToString()
                );

                await repo.AddAsync(message, ct);

                try
                {
                    await emailChannel.SendAsync(message, ct);
                    message.MarkSent();
                    await repo.UpdateAsync(message, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);
                }
                catch (Exception ex)
                {
                    message.MarkFailed(ex.Message);
                    await repo.UpdateAsync(message, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);
                    // continua para os demais, não falha o lote todo
                }
            }
        }

        // 2)  Envia por WhatsApp quando você registrar o canal "whatsapp"
        // if (whatsappChannel is not null)
        // {
        //     // aqui você pode mandar uma mensagem de boas-vindas e/ou o link diretamente para os membros
        //     // amanhã, quando plugar Z-API, também vai adicionar os PhoneE164 no grupo criado.
        // }

        // 3) Publica retorno de sucesso (canal "email" por enquanto)
        await publisher.PublishAsync(
            type: EventTypes.FamilyGroupCreatedV1,
            source: "sam.notification",
            data: new FamilyGroupCreatedV1(
                RetreatId: req.RetreatId,
                FamilyId:  req.FamilyId,
                Channel:   "email",          // hoje email; amanhã "whatsapp" quando criar grupo real
                Link:      inviteLink,
                ExternalId: externalId,
                CreatedAt: DateTimeOffset.UtcNow
            ),
            traceId: null,
            ct: ct
        );
    }
}
