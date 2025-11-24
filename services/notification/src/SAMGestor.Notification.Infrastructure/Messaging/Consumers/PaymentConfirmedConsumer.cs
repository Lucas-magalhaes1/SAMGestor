using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;
using SAMGestor.Notification.Infrastructure.Persistence;

namespace SAMGestor.Notification.Infrastructure.Messaging.Consumers;

public sealed class PaymentConfirmedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<PaymentConfirmedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.confirmed";
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
    var db        = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var repo      = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
    var renderer  = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
    var channels  = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationChannel>>();
    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

    var sel = await db.SelectedRegistrations.AsNoTracking()
        .SingleOrDefaultAsync(x => x.RegistrationId == evt.RegistrationId, ct);

    if (sel is null) return;

    string subjectTpl, bodyTpl, templateKey;

    if (sel.Kind == SAMGestor.Notification.Domain.Enums.SelectionKind.Serving)
{
    templateKey = "serving-payment-approved";
    subjectTpl  = "Equipe de serviço confirmada ✅";
    bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Equipe de serviço confirmada</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:24px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:4px;">
              Retiro · Equipe de Serviço
            </td>
          </tr>

          <tr>
            <td style="font-size:20px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Que alegria ter você com a gente! Seu pagamento foi confirmado e sua participação na
              equipe de serviço está garantida. 🙌
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:12px 16px; background-color:#f9fafb;
                       border-radius:6px; border:1px solid #e5e7eb; margin-bottom:16px;">
              <strong>Valor:</strong> {{Amount}} BRL<br />
              <strong>Método de pagamento:</strong> {{Method}}<br />
              <strong>Data de pagamento:</strong> {{PaidAt}}
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Em breve compartilharemos mais detalhes sobre os horários, funções e orientações para a equipe.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:16px; border-top:1px solid #e5e7eb;">
              Qualquer dúvida, é só responder este e-mail.<br />
              <span style="color:#6b7280;">Equipe de coordenação do retiro</span>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
}
else
{
    templateKey = "payment-approved";
    subjectTpl  = "Pagamento aprovado ✅";
    bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Pagamento aprovado</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:24px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:4px;">
              Retiro · Confirmação de Inscrição
            </td>
          </tr>

          <tr>
            <td style="font-size:20px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Boa notícia: recebemos o seu pagamento e sua inscrição para o retiro está confirmada! 🙌
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:12px 16px; background-color:#f9fafb;
                       border-radius:6px; border:1px solid #e5e7eb; margin-bottom:16px;">
              <strong>Valor:</strong> {{Amount}} BRL<br />
              <strong>Método de pagamento:</strong> {{Method}}<br />
              <strong>Data de pagamento:</strong> {{PaidAt}}
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Faltando alguns dias para o retiro, enviaremos um novo e-mail com instruções práticas,
              horários e o que levar.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:16px; border-top:1px solid #e5e7eb;">
              Se tiver qualquer dúvida até lá, é só responder esta mensagem.<br />
              <span style="color:#6b7280;">Equipe de coordenação do retiro</span>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
}


    var data = new Dictionary<string, string>
    {
        ["Name"]   = sel.Name,
        ["Amount"] = evt.Amount.ToString("0.00"),
        ["Method"] = evt.Method ?? "pix",
        ["PaidAt"] = evt.PaidAt.ToLocalTime().ToString("g")
    };

    var subject = renderer.Render(subjectTpl, data);
    var body    = renderer.Render(bodyTpl, data);

    var message = new NotificationMessage(
        channel: NotificationChannel.Email,
        recipientName: sel.Name,
        recipientEmail: sel.Email,
        recipientPhone: sel.Phone,
        templateKey: templateKey,
        subject: subject,
        body: body,
        registrationId: evt.RegistrationId,
        retreatId: evt.RetreatId,
        externalCorrelationId: evt.PaymentId.ToString()
    );

    await repo.AddAsync(message, ct);

    var emailChannel = channels.Single(c => c.Name == "email");

    try
    {
        await emailChannel.SendAsync(message, ct);

        message.MarkSent();
        await repo.UpdateAsync(message, ct);
        await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);

        await publisher.PublishAsync(
            type: EventTypes.NotificationEmailSentV1,
            source: "sam.notification",
            data: new NotificationEmailSentV1(message.Id, evt.RegistrationId, sel.Email, DateTimeOffset.UtcNow));
    }
    catch (Exception ex)
    {
        message.MarkFailed(ex.Message);
        await repo.UpdateAsync(message, ct);
        await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

        await publisher.PublishAsync(
            type: EventTypes.NotificationEmailFailedV1,
            source: "sam.notification",
            data: new NotificationEmailFailedV1(message.Id, evt.RegistrationId, sel.Email, ex.Message, DateTimeOffset.UtcNow));
    }
}

}
