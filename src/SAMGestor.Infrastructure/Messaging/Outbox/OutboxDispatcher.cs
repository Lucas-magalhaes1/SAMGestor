using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql; 
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Messaging.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly EventPublisher _publisher;
    private readonly IConfiguration _cfg;

    public OutboxDispatcher(
        IServiceProvider sp,
        ILogger<OutboxDispatcher> logger,
        EventPublisher publisher,
        IConfiguration cfg)
    {
        _sp = sp;
        _logger = logger;
        _publisher = publisher;
        _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize   = _cfg.GetValue("Outbox:BatchSize", 50);
        var pollSec     = _cfg.GetValue("Outbox:PollIntervalSeconds", 5);
        var useListen   = _cfg.GetValue("Outbox:UseListenNotify", false);
        var channelName = _cfg.GetValue("Outbox:ListenChannel", "outbox_new");
        var watchdogSec = _cfg.GetValue("Outbox:WatchdogSeconds", 30);

        _logger.LogInformation("OutboxDispatcher started. batch={batch} poll={poll}s listen={listen}",
            batchSize, pollSec, useListen);

        NpgsqlConnection? listenConn = null;
        if (useListen)
        {
            var connStr = _cfg.GetConnectionString("Default")
                         ?? throw new InvalidOperationException("ConnectionStrings:Default not configured");
            listenConn = new NpgsqlConnection(connStr);
            await listenConn.OpenAsync(stoppingToken);
            await using (var cmd = new NpgsqlCommand($"LISTEN {channelName};", listenConn))
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            _logger.LogInformation("LISTEN {channel} armed.", channelName);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var processed = await ProcessBatchAsync(batchSize, stoppingToken);
                if (processed)
                    continue;

                if (!useListen)
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollSec), stoppingToken);
                    continue;
                }

                // LISTEN mode: aguarda NOTIFY ou watchdog
                var waitTask = listenConn!.WaitAsync(stoppingToken); // acorda em NOTIFY/keepalive
                var watchdog = Task.Delay(TimeSpan.FromSeconds(watchdogSec), stoppingToken);
                await Task.WhenAny(waitTask, watchdog);
                // volta ao loop e tenta processar um novo batch
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OutboxDispatcher fatal error.");
        }
        finally
        {
            if (listenConn is not null)
                await listenConn.DisposeAsync();
            _logger.LogInformation("OutboxDispatcher stopped.");
        }
    }

    private async Task<bool> ProcessBatchAsync(int batchSize, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();

        var batch = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
            return false;

        foreach (var msg in batch)
        {
            try
            {
                await _publisher.PublishAsync(msg.Type, msg.Data, ct);
                msg.ProcessedAt = DateTimeOffset.UtcNow;
                msg.Attempts += 1;
                msg.LastError = null;
            }
            catch (Exception ex)
            {
                msg.Attempts += 1;
                msg.LastError = ex.Message;
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
