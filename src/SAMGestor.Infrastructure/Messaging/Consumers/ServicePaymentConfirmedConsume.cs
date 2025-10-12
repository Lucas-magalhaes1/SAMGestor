using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Messaging.Options;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Messaging.Consumers;

public sealed class ServicePaymentConfirmedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<ServicePaymentConfirmedConsumer> logger,
    IServiceProvider sp,
    ServiceAutoAssignOptions autoOpt
) : BackgroundService
{
    private string QueueName => string.IsNullOrWhiteSpace(opt.ServingPaymentQueue)
        ? "core.payment.serving"
        : opt.ServingPaymentQueue;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ServicePaymentConfirmedConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.PaymentConfirmedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("ServicePaymentConfirmedConsumer listening on {queue}", QueueName);

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
                        var env  = JsonSerializer.Deserialize<EventEnvelope<PaymentConfirmedV1>>(json, JsonOpts);

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
                logger.LogError(ex, "ServicePaymentConfirmedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("ServicePaymentConfirmedConsumer stopped.");
    }

    private async Task HandleAsync(PaymentConfirmedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();

        
        var reg = await db.ServiceRegistrations
            .SingleOrDefaultAsync(x => x.Id == evt.RegistrationId, ct);

        if (reg is null)
        {
            
            return;
        }

      
        var method = NormalizeMethod(evt.Method);
        var money  = new Money(evt.Amount, "BRL");

        var corePayment = await db.Payments
            .SingleOrDefaultAsync(p => p.Id == evt.PaymentId, ct);

        if (corePayment is null)
        {
            corePayment = new SAMGestor.Domain.Entities.Payment(
                registrationId: evt.RegistrationId,
                amount: money,
                method: method
            );

            
            var idProp = typeof(SAMGestor.Domain.Entities.Payment)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            idProp?.SetValue(corePayment, evt.PaymentId);

            await db.Payments.AddAsync(corePayment, ct);
        }
        else
        {
            
        }

        if (corePayment.Status != PaymentStatus.Paid)
        {
            corePayment.MarkAsPaid();
        }

        if (evt.PaidAt != default)
        {
            var paidAtProp = typeof(SAMGestor.Domain.Entities.Payment)
                .GetProperty("PaidAt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            paidAtProp?.SetValue(corePayment, evt.PaidAt.UtcDateTime);
        }

        var linkExists = await db.ServiceRegistrationPayments
            .AnyAsync(x => x.ServiceRegistrationId == reg.Id && x.PaymentId == evt.PaymentId, ct);

        if (!linkExists)
        {
            await db.ServiceRegistrationPayments.AddAsync(
                new ServiceRegistrationPayment(reg.Id, evt.PaymentId), ct);
        }

       
        if (reg.Status != ServiceRegistrationStatus.Confirmed)
        {
            try { reg.Confirm(); } catch { /* Cancelled/Declined – ignora */ }
        }

        
        if (autoOpt.Enabled)
        {
            await TryAutoAssignAsync(db, reg, autoOpt, ct);
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

    /// <summary>
    /// Tenta alocar no espaço preferido respeitando lock e, se configurado, capacidade máxima.
    /// Sem efeito se não houver PreferredSpaceId ou se já estiver alocado.
    /// </summary>
    private static async Task TryAutoAssignAsync(
        SAMContext db,
        ServiceRegistration reg,
        ServiceAutoAssignOptions opt,
        CancellationToken ct)
    {
        
        var alreadyAssigned = await db.ServiceAssignments
            .AnyAsync(a => a.ServiceRegistrationId == reg.Id, ct);
        if (alreadyAssigned) return;

        var prefId = reg.PreferredSpaceId;
        if (prefId is null) return;
        
        var space = await db.ServiceSpaces
            .SingleOrDefaultAsync(s => s.Id == prefId.Value, ct);
        if (space is null) return;

        
        if (GetBoolProperty(space, "IsLocked") is true || GetBoolProperty(space, "Locked") is true)
            return;

        
        if (opt.EnforceMax)
        {
            var max = GetIntProperty(space, "MaxPeople") 
                   ?? GetIntProperty(space, "Max")
                   ?? GetIntProperty(space, "MaxCapacity")
                   ?? GetIntProperty(space, "MaxSlots")
                   ?? GetIntProperty(space, "CapacityMax");

            if (max.HasValue)
            {
                var current = await db.ServiceAssignments
                    .CountAsync(a => a.ServiceSpaceId == space.Id, ct);

                if (current >= max.Value) return; 
            }
        }

        var assignment = new ServiceAssignment(
            serviceSpaceId: space.Id,
            serviceRegistrationId: reg.Id,
            role: ServiceRole.Member
        );
        await db.ServiceAssignments.AddAsync(assignment, ct);
        
        var retreat = await db.Retreats.SingleOrDefaultAsync(r => r.Id == reg.RetreatId, ct);
        retreat?.BumpServiceSpacesVersion();
    }

    private static bool? GetBoolProperty(object obj, string name)
    {
        var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p is null || p.PropertyType != typeof(bool)) return null;
        return (bool)p.GetValue(obj)!;
    }

    private static int? GetIntProperty(object obj, string name)
    {
        var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p is null) return null;

        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        if (t == typeof(int))   return (int?)p.GetValue(obj);
        if (t == typeof(short)) return (short?)p.GetValue(obj);
        if (t == typeof(long))  return (int?)Convert.ChangeType(p.GetValue(obj), typeof(int));
        return null;
    }
}
