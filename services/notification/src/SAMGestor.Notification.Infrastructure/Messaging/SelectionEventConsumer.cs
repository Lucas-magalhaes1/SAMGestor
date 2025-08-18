using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Orchestrators;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class SelectionEventConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true 
    };

    private readonly RabbitMqOptions _opt;
    private readonly RabbitMqConnection _conn;
    private readonly ILogger<SelectionEventConsumer> _logger;
    private readonly IServiceProvider _sp;
    private IChannel? _channel;

    public SelectionEventConsumer(
        RabbitMqOptions opt,
        RabbitMqConnection conn,
        ILogger<SelectionEventConsumer> logger,
        IServiceProvider sp)
    {
        _opt = opt;
        _conn = conn;
        _logger = logger;
        _sp = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _conn.GetOrCreateAsync().ConfigureAwait(false);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

        // garante infraestrutura
        await _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue: _opt.SelectionQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        // bind por config…
        await _channel.QueueBindAsync(_opt.SelectionQueue, _opt.Exchange, _opt.SelectionRoutingKey, cancellationToken: stoppingToken);
        // …e também a chave da versão v1 do contrato
        await _channel.QueueBindAsync(_opt.SelectionQueue, _opt.Exchange, EventTypes.SelectionParticipantSelectedV1, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var envelope = JsonSerializer.Deserialize<EventEnvelope<SelectionParticipantSelectedV1>>(json, JsonOpts);

                if (envelope?.Data is null)
                {
                    _logger.LogWarning("Invalid event envelope or null data. DeliveryTag={tag}", ea.DeliveryTag);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                // resolve serviços scoped por mensagem
                using var scope = _sp.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<NotificationOrchestrator>();

                await orchestrator.OnParticipantSelectedAsync(envelope.Data, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing selection event. DeliveryTag={tag}", ea.DeliveryTag);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);
        await _channel.BasicConsumeAsync(queue: _opt.SelectionQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("SelectionEventConsumer is listening on queue {queue}", _opt.SelectionQueue);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken);
                await _channel.DisposeAsync();
            }
        }
        catch { /* ignore */ }

        await base.StopAsync(cancellationToken);
    }
}
