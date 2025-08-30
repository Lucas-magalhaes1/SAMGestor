using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.Infrastructure.Messaging.Outbox;

public sealed class OutboxDispatcher(
    IServiceProvider sp,
    ILogger<OutboxDispatcher> logger,
    EventPublisher publisher
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Payment OutboxDispatcher started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                var batch = await db.Set<OutboxMessage>()
                    .Where(x => x.ProcessedAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var msg in batch)
                {
                    try
                    {
                        await publisher.PublishAsync(msg.Type, msg.Data, stoppingToken);
                        msg.ProcessedAt = DateTimeOffset.UtcNow;
                        msg.Attempts += 1;
                        msg.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts += 1;
                        msg.LastError = ex.Message;
                        // fica sem ProcessedAt para retry no próximo ciclo
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment OutboxDispatcher loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        logger.LogInformation("Payment OutboxDispatcher stopped.");
    }
}
