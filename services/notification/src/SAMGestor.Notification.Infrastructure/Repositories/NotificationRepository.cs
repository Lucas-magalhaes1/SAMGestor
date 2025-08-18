using Microsoft.EntityFrameworkCore;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Infrastructure.Persistence;

namespace SAMGestor.Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(NotificationMessage message, CancellationToken ct)
    {
        await _db.NotificationMessages.AddAsync(message, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationMessage message, CancellationToken ct)
    {
        _db.NotificationMessages.Update(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddLogAsync(NotificationDispatchLog log, CancellationToken ct)
    {
        await _db.NotificationDispatchLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }
}